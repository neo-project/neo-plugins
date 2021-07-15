using System;
using System.Linq;
using static Neo.Utility;

namespace Neo.FileStorage.Storage.Services.Reputaion
{
    public class PeerID : IEquatable<PeerID>
    {
        public const int PeerIDLength = 33;
        private readonly byte[] value;

        public PeerID(byte[] bytes)
        {
            if (bytes is null || bytes.Length != PeerIDLength)
                throw new ArgumentException("invalid PeerID bytes", nameof(bytes));
            value = bytes;
        }

        public static PeerID FromString(string idString)
        {
            return new PeerID(StrictUTF8.GetBytes(idString));
        }

        public byte[] ToByteArray()
        {
            return value;
        }

        public override string ToString()
        {
            return Convert.ToBase64String(value);
        }

        public bool Equals(PeerID other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return value.SequenceEqual(other.value);
        }

        public static implicit operator byte[](PeerID p)
        {
            return p.value;
        }

        public static implicit operator PeerID(byte[] bytes)
        {
            return new PeerID(bytes);
        }
    }
}
