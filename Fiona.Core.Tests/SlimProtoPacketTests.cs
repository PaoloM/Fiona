using System;
using System.Text;

using Fiona.Core.Services.SlimProto;

using Xunit;

namespace Fiona.Core.Tests
{
    /// <summary>
    /// Pins the SlimProto wire format against squeezelite's slimproto.h, which is the reference
    /// implementation. These are offset-for-offset assertions on purpose: a wrong offset compiles
    /// perfectly happily and then the server quietly ignores the player, which is a miserable thing
    /// to debug. Offsets here are into the framed packet, because that is what goes on the wire.
    /// </summary>
    public class SlimProtoPacketTests
    {
        private static readonly byte[] Mac = new byte[] { 0x02, 0x11, 0x22, 0x33, 0x44, 0x55 };

        #region Framing

        [Fact]
        public void Frame_PutsTheOpcodeFirst()
        {
            byte[] packet = SlimProtoPackets.Frame("STAT", new byte[3]);

            Assert.Equal("STAT", Encoding.ASCII.GetString(packet, 0, 4));
        }

        [Fact]
        public void Frame_CountsThePayloadOnlyInTheLength()
        {
            // The client-to-server length excludes the eight header bytes, which is the opposite of
            // the server-to-client framing. Getting this backwards desynchronises the connection.
            byte[] packet = SlimProtoPackets.Frame("STAT", new byte[53]);

            Assert.Equal(61, packet.Length);
            Assert.Equal(53u, ReadUInt32(packet, 4));
        }

        #endregion

        #region HELO

        [Fact]
        public void Helo_IsFortyFourBytesPlusTheCapabilities()
        {
            byte[] packet = SlimProtoPackets.Frame("HELO", SlimProtoPackets.Helo(Mac, false, "abc"));

            Assert.Equal(44 + 3, packet.Length);
        }

        [Fact]
        public void Helo_IdentifiesAsASoftwarePlayer()
        {
            byte[] packet = SlimProtoPackets.Frame("HELO", SlimProtoPackets.Helo(Mac, false, ""));

            Assert.Equal(12, packet[8]); // deviceid, squeezeplay
        }

        [Fact]
        public void Helo_CarriesTheMacAtTheStructOffset()
        {
            byte[] packet = SlimProtoPackets.Frame("HELO", SlimProtoPackets.Helo(Mac, false, ""));

            // mac[6] sits at struct offset 10, after opcode, length, deviceid and revision.
            Assert.Equal(Mac, Slice(packet, 10, 6));
        }

        [Fact]
        public void Helo_MarksAReconnectInTheChannelList()
        {
            byte[] fresh = SlimProtoPackets.Frame("HELO", SlimProtoPackets.Helo(Mac, false, ""));
            byte[] again = SlimProtoPackets.Frame("HELO", SlimProtoPackets.Helo(Mac, true, ""));

            Assert.Equal(0x0000, ReadUInt16(fresh, 32));
            Assert.Equal(0x4000, ReadUInt16(again, 32));
        }

        [Fact]
        public void Helo_EndsWithTheCapabilityString()
        {
            const string caps = "Model=squeezelite,mp3,flc";

            byte[] packet = SlimProtoPackets.Frame("HELO", SlimProtoPackets.Helo(Mac, false, caps));

            Assert.Equal(caps, Encoding.ASCII.GetString(packet, 44, packet.Length - 44));
        }

        [Fact]
        public void Helo_RefusesAMacThatIsNotSixBytes()
        {
            Assert.Throws<ArgumentException>(() => SlimProtoPackets.Helo(new byte[5], false, ""));
        }

        #endregion

        #region STAT

        [Fact]
        public void Stat_IsSixtyOneBytesOnTheWire()
        {
            byte[] packet = Stat("STMt", TimeSpan.Zero, 0);

            Assert.Equal(61, packet.Length);
        }

        [Fact]
        public void Stat_CarriesTheEventCode()
        {
            byte[] packet = Stat("STMd", TimeSpan.Zero, 0);

            Assert.Equal("STMd", Encoding.ASCII.GetString(packet, 8, 4));
        }

        [Fact]
        public void Stat_ReportsElapsedTimeInBothSecondsAndMilliseconds()
        {
            // The server takes its progress bar from these, so both have to agree.
            byte[] packet = Stat("STMt", TimeSpan.FromMilliseconds(93_500), 0);

            Assert.Equal(93u, ReadUInt32(packet, 45));      // elapsed_seconds
            Assert.Equal(93_500u, ReadUInt32(packet, 51));  // elapsed_milliseconds
        }

        [Fact]
        public void Stat_EchoesTheServerTimestampUntouched()
        {
            // The server measures the round trip with this. Altering it makes the measurement wrong.
            byte[] packet = Stat("STMt", TimeSpan.Zero, 0xDEADBEEF);

            Assert.Equal(0xDEADBEEFu, ReadUInt32(packet, 55));
        }

        [Fact]
        public void Stat_ClaimsFullSignalStrengthForAWiredPlayer()
        {
            byte[] packet = Stat("STMt", TimeSpan.Zero, 0);

            Assert.Equal(0xFFFF, ReadUInt16(packet, 31));
        }

        [Fact]
        public void Stat_ReportsTheBufferTwiceOverForStreamAndOutput()
        {
            byte[] payload = SlimProtoPackets.Stat("STMt", 1000, 250, 0, TimeSpan.Zero, 0);
            byte[] packet = SlimProtoPackets.Frame("STAT", payload);

            Assert.Equal(1000u, ReadUInt32(packet, 15)); // stream_buffer_size
            Assert.Equal(250u, ReadUInt32(packet, 19));  // stream_buffer_fullness
            Assert.Equal(1000u, ReadUInt32(packet, 37)); // output_buffer_size
            Assert.Equal(250u, ReadUInt32(packet, 41));  // output_buffer_fullness
        }

        #endregion

        #region strm

        [Fact]
        public void Strm_ReadsTheCommandAndAutostart()
        {
            byte[] packet = Strm('s', autostart: '1');

            Assert.Equal('s', SlimProtoPackets.StrmCommand(packet));
            Assert.Equal(1, SlimProtoPackets.StrmAutostart(packet));
        }

        [Fact]
        public void Strm_ReadsAutostartAsAnAsciiDigit()
        {
            // Sent as '0', not 0. Reading the raw byte would make every stream look self-starting.
            Assert.Equal(0, SlimProtoPackets.StrmAutostart(Strm('s', autostart: '0')));
        }

        [Fact]
        public void Strm_ReadsTheReplayGainFieldThatOtherCommandsReuse()
        {
            byte[] packet = Strm('t', autostart: '0');
            WriteUInt32(packet, 18, 0x01020304);

            Assert.Equal(0x01020304u, SlimProtoPackets.StrmReplayGain(packet));
        }

        [Fact]
        public void StreamUri_UsesTheAddressAndPortFromThePacket()
        {
            byte[] packet = Strm('s', '1', "GET /stream.mp3?player=02:11 HTTP/1.0\r\n\r\n",
                                 ip: new byte[] { 10, 0, 0, 7 }, port: 9002);

            Uri uri = SlimProtoPackets.StreamUri(packet, "192.168.1.1", 9000);

            Assert.Equal("http://10.0.0.7:9002/stream.mp3?player=02:11", uri.ToString());
        }

        [Fact]
        public void StreamUri_FallsBackToTheConnectedServerWhenTheAddressIsZero()
        {
            // A zero address means "the server you are already talking to".
            byte[] packet = Strm('s', '1', "GET /stream.mp3 HTTP/1.0\r\n\r\n",
                                 ip: new byte[] { 0, 0, 0, 0 }, port: 0);

            Uri uri = SlimProtoPackets.StreamUri(packet, "192.168.1.1", 9000);

            Assert.Equal("http://192.168.1.1:9000/stream.mp3", uri.ToString());
        }

        [Fact]
        public void StreamUri_AcceptsAnAbsoluteRequestTarget()
        {
            // A request line may carry a whole URL rather than a path. Appending that to a host
            // produces a valid-looking address that points nowhere.
            byte[] packet = Strm('s', '1', "GET http://elsewhere:8080/live.mp3 HTTP/1.0\r\n\r\n",
                                 ip: new byte[] { 10, 0, 0, 7 }, port: 9000);

            Uri uri = SlimProtoPackets.StreamUri(packet, "192.168.1.1", 9000);

            Assert.Equal("http://elsewhere:8080/live.mp3", uri.ToString());
        }

        [Fact]
        public void StreamUri_IsNullWhenThereIsNoRequestAtAll()
        {
            // Every strm carries the full header, but only 's' appends a request.
            Assert.Null(SlimProtoPackets.StreamUri(Strm('t', '0'), "192.168.1.1", 9000));
        }

        [Fact]
        public void StreamUri_IsNullWhenTheRequestLineMakesNoSense()
        {
            byte[] packet = Strm('s', '1', "nonsense\r\n\r\n",
                                 ip: new byte[] { 10, 0, 0, 7 }, port: 9000);

            Assert.Null(SlimProtoPackets.StreamUri(packet, "192.168.1.1", 9000));
        }

        #endregion

        #region audg and aude

        [Fact]
        public void AudgVolume_ReadsSixteenSixteenFixedPoint()
        {
            Assert.Equal(0.5, SlimProtoPackets.AudgVolume(Audg(adjust: 1, left: 32768, right: 32768)), 3);
            Assert.Equal(1.0, SlimProtoPackets.AudgVolume(Audg(adjust: 1, left: 65536, right: 65536)), 3);
            Assert.Equal(0.0, SlimProtoPackets.AudgVolume(Audg(adjust: 1, left: 0, right: 0)), 3);
        }

        [Fact]
        public void AudgVolume_IsFullScaleWhenTheServerIsNotSettingAVolume()
        {
            Assert.Equal(1.0, SlimProtoPackets.AudgVolume(Audg(adjust: 0, left: 0, right: 0)), 3);
        }

        [Fact]
        public void AudgVolume_TakesTheLouderOfTheTwoChannels()
        {
            // One output, so a balance has to collapse to something rather than silence a channel.
            Assert.Equal(1.0, SlimProtoPackets.AudgVolume(Audg(adjust: 1, left: 0, right: 65536)), 3);
        }

        [Fact]
        public void AudgVolume_ClampsGainAboveUnity()
        {
            // The server can ask for more than full scale; the sink cannot give it.
            Assert.Equal(1.0, SlimProtoPackets.AudgVolume(Audg(adjust: 1, left: 200000, right: 0)), 3);
        }

        [Fact]
        public void AudeOutputEnabled_AcceptsEitherFlag()
        {
            // The regression this pins: the reference implementation reads enable_spdif and ignores
            // enable_dac, but a player advertising no digital out cannot count on either one being
            // the flag the server sets. Reading only enable_dac pinned the volume to zero.
            Assert.True(SlimProtoPackets.AudeOutputEnabled(Aude(spdif: 1, dac: 0)));
            Assert.True(SlimProtoPackets.AudeOutputEnabled(Aude(spdif: 0, dac: 1)));
            Assert.True(SlimProtoPackets.AudeOutputEnabled(Aude(spdif: 1, dac: 1)));
        }

        [Fact]
        public void AudeOutputEnabled_IsFalseOnlyWhenBothFlagsAreClear()
        {
            Assert.False(SlimProtoPackets.AudeOutputEnabled(Aude(spdif: 0, dac: 0)));
        }

        #endregion

        #region setd

        [Fact]
        public void Setd_WithNoDataIsTheServerAskingForTheName()
        {
            string name;
            bool ours = SlimProtoPackets.TryReadSetdName(new byte[] { 0x73, 0x65, 0x74, 0x64, 0 }, out name);

            Assert.True(ours);
            Assert.Null(name);
        }

        [Fact]
        public void Setd_WithDataIsTheServerRenamingUs()
        {
            byte[] packet = Setd("Kitchen");

            string name;
            Assert.True(SlimProtoPackets.TryReadSetdName(packet, out name));
            Assert.Equal("Kitchen", name);
        }

        [Fact]
        public void Setd_IgnoresSettingsOtherThanTheName()
        {
            byte[] packet = Setd("Kitchen");
            packet[4] = 4; // some other setting id

            string name;
            Assert.False(SlimProtoPackets.TryReadSetdName(packet, out name));
        }

        [Fact]
        public void SetdName_IsTerminated()
        {
            // The server reads this as a C string.
            byte[] payload = SlimProtoPackets.SetdName("Hi");

            Assert.Equal(new byte[] { 0, (byte)'H', (byte)'i', 0 }, payload);
        }

        #endregion

        #region Builders for synthetic server packets

        private static byte[] Stat(string code, TimeSpan position, uint serverTimestamp)
        {
            return SlimProtoPackets.Frame("STAT",
                SlimProtoPackets.Stat(code, 0, 0, 0, position, serverTimestamp));
        }

        private static byte[] Strm(char command, char autostart, string request = null,
                                   byte[] ip = null, int port = 0)
        {
            byte[] requestBytes = request == null
                ? new byte[0]
                : Encoding.ASCII.GetBytes(request);

            var packet = new byte[SlimProtoPackets.StrmHeaderLength + requestBytes.Length];
            Encoding.ASCII.GetBytes("strm", 0, 4, packet, 0);

            packet[4] = (byte)command;
            packet[5] = (byte)autostart;
            packet[6] = (byte)'m'; // format: mp3

            WriteUInt16(packet, 22, (ushort)port);
            if (ip != null) Array.Copy(ip, 0, packet, 24, 4);

            Array.Copy(requestBytes, 0, packet, SlimProtoPackets.StrmHeaderLength, requestBytes.Length);
            return packet;
        }

        private static byte[] Audg(byte adjust, uint left, uint right)
        {
            var packet = new byte[22];
            Encoding.ASCII.GetBytes("audg", 0, 4, packet, 0);

            packet[12] = adjust;
            WriteUInt32(packet, 14, left);
            WriteUInt32(packet, 18, right);
            return packet;
        }

        private static byte[] Aude(byte spdif, byte dac)
        {
            var packet = new byte[6];
            Encoding.ASCII.GetBytes("aude", 0, 4, packet, 0);

            packet[4] = spdif;
            packet[5] = dac;
            return packet;
        }

        private static byte[] Setd(string name)
        {
            byte[] text = Encoding.UTF8.GetBytes(name);

            var packet = new byte[5 + text.Length];
            Encoding.ASCII.GetBytes("setd", 0, 4, packet, 0);
            packet[4] = 0;
            Array.Copy(text, 0, packet, 5, text.Length);
            return packet;
        }

        #endregion

        #region Big-endian readers, written out so the tests do not lean on the code under test

        private static int ReadUInt16(byte[] b, int offset)
        {
            return (b[offset] << 8) | b[offset + 1];
        }

        private static uint ReadUInt32(byte[] b, int offset)
        {
            return ((uint)b[offset] << 24) | ((uint)b[offset + 1] << 16)
                 | ((uint)b[offset + 2] << 8) | b[offset + 3];
        }

        private static void WriteUInt16(byte[] b, int offset, ushort value)
        {
            b[offset] = (byte)(value >> 8);
            b[offset + 1] = (byte)value;
        }

        private static void WriteUInt32(byte[] b, int offset, uint value)
        {
            b[offset] = (byte)(value >> 24);
            b[offset + 1] = (byte)(value >> 16);
            b[offset + 2] = (byte)(value >> 8);
            b[offset + 3] = (byte)value;
        }

        private static byte[] Slice(byte[] b, int offset, int length)
        {
            var slice = new byte[length];
            Array.Copy(b, offset, slice, 0, length);
            return slice;
        }

        #endregion
    }
}
