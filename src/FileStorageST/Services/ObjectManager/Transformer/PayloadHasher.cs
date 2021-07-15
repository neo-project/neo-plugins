using Neo.FileStorage.API.Cryptography.Tz;
using Neo.FileStorage.API.Refs;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Neo.FileStorage.Storage.Services.ObjectManager.Transformer
{
    public sealed class PayloadHasher : IDisposable
    {
        public readonly ChecksumType Type;

        private readonly HashAlgorithm hasher;

        public PayloadHasher(ChecksumType type)
        {
            hasher = type switch
            {
                ChecksumType.Sha256 => SHA256.Create(),
                ChecksumType.Tz => new TzHash(),
                _ => throw new InvalidOperationException($"{nameof(PayloadHasher)} not supported checksum type"),
            };
            Type = type;
        }

        public void Write(byte[] chunk)
        {
            hasher.TransformBlock(chunk, 0, chunk.Length, null, 0);
        }

        public byte[] Sum()
        {
            hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return hasher.Hash;
        }

        public void Dispose()
        {
            hasher.Dispose();
        }
    }
}
