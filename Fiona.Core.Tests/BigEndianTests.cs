using Fiona.Core.Services.SlimProto;

using Xunit;

namespace Fiona.Core.Tests
{
    /// <summary>
    /// SlimProto is network byte order throughout, and the machines this runs on are not. These
    /// assert the byte order explicitly rather than just round-tripping, because a round trip
    /// passes just as happily when both halves are little-endian.
    /// </summary>
    public class BigEndianTests
    {
        [Fact]
        public void WritesTheMostSignificantByteFirst()
        {
            var buffer = new byte[4];
            BigEndian.Write(buffer, 0, 0x01020304u);

            Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, buffer);
        }

        [Fact]
        public void WritesSixteenBitValuesTheSameWayRound()
        {
            var buffer = new byte[2];
            BigEndian.Write(buffer, 0, (ushort)0xABCD);

            Assert.Equal(new byte[] { 0xAB, 0xCD }, buffer);
        }

        [Fact]
        public void ReadsBackWhatItWrote()
        {
            var buffer = new byte[6];
            BigEndian.Write(buffer, 0, (ushort)0xFFFF);
            BigEndian.Write(buffer, 2, 0xDEADBEEFu);

            Assert.Equal(0xFFFF, BigEndian.ReadUInt16(buffer, 0));
            Assert.Equal(0xDEADBEEFu, BigEndian.ReadUInt32(buffer, 2));
        }

        [Fact]
        public void HonoursTheOffset()
        {
            var buffer = new byte[8];
            BigEndian.Write(buffer, 3, 0x11223344u);

            Assert.Equal(0u, BigEndian.ReadUInt32(buffer, 0) & 0xFFFFFF00u);
            Assert.Equal(0x11223344u, BigEndian.ReadUInt32(buffer, 3));
        }
    }
}
