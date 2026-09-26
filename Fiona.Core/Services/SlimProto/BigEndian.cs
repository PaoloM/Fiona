using System;

namespace Fiona.Core.Services.SlimProto
{
    /// <summary>
    /// SlimProto puts every multi-byte field in network byte order, and the packets are flat
    /// structs read and written at fixed offsets, so these take an offset rather than a stream.
    /// </summary>
    internal static class BigEndian
    {
        public static ushort ReadUInt16(byte[] buffer, int offset)
        {
            return (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
        }

        public static uint ReadUInt32(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset] << 24)
                 | ((uint)buffer[offset + 1] << 16)
                 | ((uint)buffer[offset + 2] << 8)
                 | buffer[offset + 3];
        }

        public static void Write(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value >> 8);
            buffer[offset + 1] = (byte)value;
        }

        public static void Write(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }
    }
}
