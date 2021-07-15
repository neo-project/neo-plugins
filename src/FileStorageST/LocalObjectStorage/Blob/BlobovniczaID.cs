using System;
using System.Linq;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Blob
{
    public class BlobovniczaID : IEquatable<BlobovniczaID>
    {
        private readonly byte[] value = Array.Empty<byte>();

        public bool IsEmpty => !value.Any();

        public BlobovniczaID(byte[] bytes)
        {
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
            value = bytes;
        }

        public override string ToString()
        {
            return Utility.StrictUTF8.GetString(value);
        }

        public bool Equals(BlobovniczaID other)
        {
            if (other is null) return false;
            return value.SequenceEqual(other.value);
        }

        public static implicit operator BlobovniczaID(byte[] val)
        {
            if (val is null) return null;
            return new BlobovniczaID(val);
        }

        public static implicit operator BlobovniczaID(string str)
        {
            return new BlobovniczaID(Utility.StrictUTF8.GetBytes(str));
        }

        public static implicit operator byte[](BlobovniczaID b)
        {
            return b.value;
        }

        public static implicit operator string(BlobovniczaID b)
        {
            return b.ToString();
        }
    }
}
