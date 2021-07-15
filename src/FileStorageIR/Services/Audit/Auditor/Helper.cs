using System;

namespace Neo.FileStorage.InnerRing.Services.Audit.Auditor
{
    public static class Helper
    {
        public static ulong RandomUInt64(ulong max = ulong.MaxValue)
        {
            var random = new Random();
            var buffer = new byte[sizeof(ulong)];
            random.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer) % max;
        }
    }
}
