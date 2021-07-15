using ZstdNet;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Blob
{
    public sealed class ZstdCompressor : ICompressor
    {
        public byte[] Compress(byte[] data)
        {
            using Compressor compressor = new();
            return compressor.Wrap(data);
        }

        public byte[] Decompress(byte[] data)
        {
            using Decompressor decompressor = new();
            return decompressor.Unwrap(data);
        }
    }
}
