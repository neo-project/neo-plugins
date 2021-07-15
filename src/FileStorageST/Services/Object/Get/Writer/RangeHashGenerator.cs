using System;
using Neo.Cryptography;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.Utils;
using static Neo.Helper;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Get.Writer
{
    public class RangeHashGenerator : IObjectResponseWriter
    {
        private ChecksumType type;
        private byte[] data = Array.Empty<byte>();

        public RangeHashGenerator(ChecksumType type)
        {
            this.type = type;
        }

        public void WriteHeader(FSObject obj)
        {
            throw new NotImplementedException($"{nameof(RangeHashGenerator)} should not write header when get range hash");
        }

        public void WriteChunk(byte[] chunk)
        {
            data = Concat(data, chunk);
        }

        public byte[] GetHash()
        {
            switch (type)
            {
                case ChecksumType.Sha256:
                    return data.Sha256();
                case ChecksumType.Tz:
                    return data;
                default:
                    throw new InvalidOperationException(nameof(RangeHashGenerator) + " unsupported hash type");
            }
        }
    }
}
