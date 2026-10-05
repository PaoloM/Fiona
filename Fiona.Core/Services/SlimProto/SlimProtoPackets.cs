using System;
using System.Text;

namespace Fiona.Core.Services.SlimProto
{
    /// <summary>
    /// The wire format, by itself: builds the packets we send and reads the fields out of the ones
    /// we receive, with no socket and no state anywhere near it.
    ///
    /// Separated from <see cref="SlimProtoClient"/> because this is the part that is easy to get
    /// silently wrong and impossible to check by reading. Every layout here is taken field for field
    /// from squeezelite's slimproto.h, the reference implementation, and the offsets are pinned by
    /// tests - which is the only reason to trust them, since a wrong offset does not fail to
    /// compile, it just makes the server quietly ignore us.
    ///
    /// The framing is asymmetric:
    ///   server -> client:  [u16 length][4-byte opcode][payload]   length covers opcode + payload
    ///   client -> server:  [4-byte opcode][u32 length][payload]   length covers the payload only
    ///
    /// Every multi-byte field is big-endian.
    /// </summary>
    internal static class SlimProtoPackets
    {
        /// <summary>Device id 12, which is what software players identify as.</summary>
        public const byte DeviceIdSqueezePlay = 12;

        /// <summary>
        /// struct strm_packet is this long including its opcode; the HTTP request the server wants
        /// us to make follows it.
        /// </summary>
        public const int StrmHeaderLength = 28;

        // Offsets within a strm packet, counting the opcode:
        //   4 command   6 format            11 threshold        15 flags      18 replay_gain (u32)
        //   5 autostart 7 pcm_sample_size   12 spdif_enable     16 output_threshold
        //               8 pcm_sample_rate   13 transition_period 17 slaves    22 server_port (u16)
        //               9 pcm_channels      14 transition_type               24 server_ip (u32)
        //              10 pcm_endianness
        private const int StrmCommandOffset = 4;
        private const int StrmAutostartOffset = 5;
        private const int StrmReplayGainOffset = 18;
        private const int StrmServerPortOffset = 22;
        private const int StrmServerIpOffset = 24;

        #region Client to server

        /// <summary>
        /// Wraps a payload in the client-to-server framing. The length counts the payload alone,
        /// not the eight bytes of header - which is the opposite of the server's own framing.
        /// </summary>
        public static byte[] Frame(string opcode, byte[] payload)
        {
            var packet = new byte[8 + payload.Length];
            Encoding.ASCII.GetBytes(opcode, 0, 4, packet, 0);
            BigEndian.Write(packet, 4, (uint)payload.Length);
            Array.Copy(payload, 0, packet, 8, payload.Length);
            return packet;
        }

        /// <summary>
        /// The HELO payload: who we are and what we can play. struct HELO_packet is 44 bytes, so the
        /// payload is the 36 after the header, plus the capability string:
        ///   0 deviceid  2 mac[6]   8 uuid[16]   24 wlan_channellist
        ///   1 revision  26 bytes_received_H   30 bytes_received_L   34 lang[2]   36 capabilities
        /// </summary>
        public static byte[] Helo(byte[] mac, bool reconnect, string capabilities)
        {
            if (mac == null || mac.Length != 6)
            {
                throw new ArgumentException("A MAC address is six bytes", nameof(mac));
            }

            byte[] caps = Encoding.ASCII.GetBytes(capabilities ?? string.Empty);
            var payload = new byte[36 + caps.Length];

            payload[0] = DeviceIdSqueezePlay;
            payload[1] = 0; // revision
            Array.Copy(mac, 0, payload, 2, 6);

            // uuid stays zero: the server does not require one.
            // The top bit of the channel list is how a reconnecting player says so.
            BigEndian.Write(payload, 24, (ushort)(reconnect ? 0x4000 : 0x0000));

            // bytes_received stays zero: no audio has arrived on this connection yet.
            payload[34] = (byte)'E';
            payload[35] = (byte)'N';

            Array.Copy(caps, 0, payload, 36, caps.Length);
            return payload;
        }

        /// <summary>
        /// The STAT payload, which is how the server learns where we are in the track. struct
        /// STAT_packet is 61 bytes, so the payload is 53. Offsets are payload-relative:
        ///   0 event[4]              15 bytes_received_H   33 output_buffer_fullness
        ///   4 num_crlf              19 bytes_received_L   37 elapsed_seconds
        ///   5 mas_initialized       23 signal_strength    41 voltage
        ///   6 mas_mode              25 jiffies            43 elapsed_milliseconds
        ///   7 stream_buffer_size    29 output_buffer_size 47 server_timestamp
        ///  11 stream_buffer_fullness                      51 error_code
        /// </summary>
        public static byte[] Stat(
            string eventCode,
            uint bufferSize,
            uint bufferFullness,
            uint jiffies,
            TimeSpan position,
            uint serverTimestamp)
        {
            var payload = new byte[53];

            Encoding.ASCII.GetBytes(eventCode, 0, 4, payload, 0);

            BigEndian.Write(payload, 7, bufferSize);
            BigEndian.Write(payload, 11, bufferFullness);

            // bytes_received stays zero: the audio is fetched by something that does not count.
            BigEndian.Write(payload, 23, (ushort)0xFFFF); // wired, so full signal strength
            BigEndian.Write(payload, 25, jiffies);
            BigEndian.Write(payload, 29, bufferSize);
            BigEndian.Write(payload, 33, bufferFullness);
            BigEndian.Write(payload, 37, (uint)position.TotalSeconds);
            BigEndian.Write(payload, 43, (uint)position.TotalMilliseconds);
            BigEndian.Write(payload, 47, serverTimestamp);

            return payload;
        }

        /// <summary>
        /// The SETD payload for the player name: setting id, then the name, then a terminator.
        /// </summary>
        public static byte[] SetdName(string name)
        {
            byte[] text = Encoding.UTF8.GetBytes(name ?? string.Empty);

            var payload = new byte[1 + text.Length + 1];
            payload[0] = 0; // id 0 is the player name
            Array.Copy(text, 0, payload, 1, text.Length);
            return payload;
        }

        #endregion

        #region Server to client

        public static char StrmCommand(byte[] packet)
        {
            return (char)packet[StrmCommandOffset];
        }

        /// <summary>
        /// Zero means the server will say when to start, with a strm 'u', once we report ourselves
        /// ready. Anything else means we start on our own. Sent as an ASCII digit.
        /// </summary>
        public static int StrmAutostart(byte[] packet)
        {
            return packet[StrmAutostartOffset] - '0';
        }

        /// <summary>
        /// The replay gain field is reused as a plain number by several commands: an interval for
        /// pause and skip, and the server's own timestamp for the 't' ping, which has to be echoed
        /// back untouched.
        /// </summary>
        public static uint StrmReplayGain(byte[] packet)
        {
            return BigEndian.ReadUInt32(packet, StrmReplayGainOffset);
        }

        /// <summary>
        /// Turns the HTTP request embedded in a strm into a URL. The request looks like
        /// "GET /stream.mp3?player=xx HTTP/1.0" followed by headers of no use to us, since whatever
        /// fetches the audio makes its own request.
        ///
        /// A request target can be an absolute URL as well as a path, and a zero server address
        /// means "the server you are already talking to". Returns null if there is nothing usable,
        /// which the caller should treat as a track it cannot play rather than an error.
        /// </summary>
        public static Uri StreamUri(byte[] packet, string fallbackHost, int fallbackPort)
        {
            if (packet.Length < StrmHeaderLength) return null;

            string request = Encoding.ASCII.GetString(
                packet, StrmHeaderLength, packet.Length - StrmHeaderLength);

            int endOfLine = request.IndexOf('\r');
            string requestLine = endOfLine >= 0 ? request.Substring(0, endOfLine) : request;

            string[] parts = requestLine.Split(' ');
            if (parts.Length < 2) return null;

            string target = parts[1];
            if (string.IsNullOrEmpty(target)) return null;

            Uri absolute;
            if (target.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(target, UriKind.Absolute, out absolute))
            {
                return absolute;
            }

            string host = fallbackHost;
            int ip = StrmServerIpOffset;
            if (packet[ip] != 0 || packet[ip + 1] != 0 || packet[ip + 2] != 0 || packet[ip + 3] != 0)
            {
                host = packet[ip] + "." + packet[ip + 1] + "." + packet[ip + 2] + "." + packet[ip + 3];
            }

            int port = BigEndian.ReadUInt16(packet, StrmServerPortOffset);
            if (port == 0) port = fallbackPort;

            Uri uri;
            string url = "http://" + host + ":" + port.ToString() + target;
            return Uri.TryCreate(url, UriKind.Absolute, out uri) ? uri : null;
        }

        /// <summary>
        /// Volume from an audg, as a linear multiplier from 0 to 1. struct audg_packet:
        ///   4 old_gainL   8 old_gainR   12 adjust   13 preamp   14 gainL   18 gainR
        /// The gains are 16.16 fixed point, so 65536 is unity. A clear adjust flag means the server
        /// is not setting a volume and full scale is correct.
        ///
        /// There is one output, so the two channels collapse to the louder of the pair, which loses
        /// a balance setting we have already told the server we do not have.
        /// </summary>
        public static double AudgVolume(byte[] packet)
        {
            if (packet.Length < 22) return 1.0;
            if (packet[12] == 0) return 1.0;

            uint left = BigEndian.ReadUInt32(packet, 14);
            uint right = BigEndian.ReadUInt32(packet, 18);

            double gain = Math.Max(left, right) / 65536.0;
            return Math.Min(1.0, Math.Max(0.0, gain));
        }

        /// <summary>
        /// Whether an aude wants us making any sound. struct aude_packet: 4 enable_spdif,
        /// 5 enable_dac.
        ///
        /// Either flag counts. Reading only one is a trap: the reference implementation keys off
        /// enable_spdif and ignores enable_dac, but a player advertising no digital output - as we
        /// do - cannot rely on the server setting the flag it happens to read.
        /// </summary>
        public static bool AudeOutputEnabled(byte[] packet)
        {
            if (packet.Length < 6) return true;

            return packet[4] != 0 || packet[5] != 0;
        }

        /// <summary>
        /// The name carried by a setd, or null when the server is asking rather than telling.
        /// struct setd_packet: 4 id, 5 data. Only id 0, the player name, is of interest.
        /// </summary>
        public static bool TryReadSetdName(byte[] packet, out string name)
        {
            name = null;

            if (packet.Length < 5 || packet[4] != 0) return false;
            if (packet.Length == 5) return true; // a query: the server wants to be told the name

            string text = Encoding.UTF8.GetString(packet, 5, packet.Length - 5).TrimEnd('\0');
            if (!string.IsNullOrEmpty(text)) name = text;

            return true;
        }

        #endregion
    }
}
