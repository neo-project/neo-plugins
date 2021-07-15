using System;
using Google.Protobuf;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Core.Object;
using Neo.FileStorage.Storage.Services.ObjectManager.Transformer;
using static Neo.Helper;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Put
{
    public class ValidationTarget : IObjectTarget
    {
        public IObjectTarget Next { get; init; }
        public ObjectValidator ObjectValidator { get; init; }

        private Checksum checksum;
        private byte[] payload = Array.Empty<byte>();

        public void WriteHeader(FSObject header)
        {
            checksum = header.Header.PayloadHash;
            if (checksum.Type != ChecksumType.Sha256 && checksum.Type != ChecksumType.Tz)
                throw new InvalidOperationException(nameof(ValidationTarget) + " unsupported paylaod checksum type " + checksum.Type);
            if (!ObjectValidator.Validate(header))
                throw new FormatException(nameof(ValidationTarget) + " invalid object");
            Next.WriteHeader(header);
        }

        public void WriteChunk(byte[] chunk)
        {
            payload = Concat(payload, chunk);
            Next.WriteChunk(chunk); //TODO: fix this may double payload memory
        }

        public AccessIdentifiers Close()
        {
            if (!checksum.Verify(ByteString.CopyFrom(payload))) throw new InvalidOperationException(nameof(ValidationTarget) + " invalid checksum");
            return Next.Close();
        }
    }
}
