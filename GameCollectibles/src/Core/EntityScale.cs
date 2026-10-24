using System;
using System.Runtime.InteropServices;
using GTA;

namespace GameCollectibles.Core
{
    internal static class EntityScale
    {
        private const int RightOffset = 0x60;
        private const int ForwardOffset = 0x70;
        private const int UpOffset = 0x80;

        public static void Apply(Entity entity, float heading, float scale)
        {
            if (entity == null || Math.Abs(scale - 1f) < 0.001f) return;
            IntPtr address = entity.MemoryAddress;
            if (address == IntPtr.Zero) return;
            double radians = heading * Math.PI / 180.0;
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);
            Write(address + RightOffset, cos * scale, sin * scale, 0f);
            Write(address + ForwardOffset, -sin * scale, cos * scale, 0f);
            Write(address + UpOffset, 0f, 0f, scale);
        }

        private static void Write(IntPtr at, float x, float y, float z)
        {
            Marshal.WriteInt32(at, BitConverter.ToInt32(BitConverter.GetBytes(x), 0));
            Marshal.WriteInt32(at + 4, BitConverter.ToInt32(BitConverter.GetBytes(y), 0));
            Marshal.WriteInt32(at + 8, BitConverter.ToInt32(BitConverter.GetBytes(z), 0));
        }
    }
}
