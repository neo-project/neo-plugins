using System;
using System.Linq;
using Neo.Cryptography;


namespace Neo.FileStorage.LocalObjectStorage.Shards
{
    public class ShardID
    {
        private readonly byte[] value;

        public bool IsEmpty => value is null || !value.Any();

        public ShardID()
        {
            value = Guid.NewGuid().ToByteArray();
        }

        public ShardID(byte[] bytes)
        {
            value = bytes;
        }

        public override string ToString()
        {
            return Base58.Encode(value);
        }

        public static implicit operator ShardID(byte[] val)
        {
            if (val is null) return null;
            return new ShardID(val);
        }

        public static implicit operator byte[](ShardID b)
        {
            return b?.value;
        }
    }
}
