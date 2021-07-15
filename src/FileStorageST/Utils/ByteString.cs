using System;
using Google.Protobuf;

namespace Neo.FileStorage.Storage.Utils
{
    public static partial class Utils
    {
        public static ByteString Concat(this ByteString a, ByteString b)
        {
            if (a is null)
                throw new ArgumentNullException(nameof(a));
            if (a is null)
                throw new ArgumentNullException(nameof(b));
            return ByteString.CopyFrom(Helper.Concat(a.ToByteArray(), b.ToByteArray()));
        }

        public static ByteString Range(this ByteString a, ulong left, ulong right)
        {
            if (a is null)
                throw new ArgumentNullException(nameof(a));
            return ByteString.CopyFrom(a.ToByteArray()[(int)left..(int)right]);
        }
    }
}
