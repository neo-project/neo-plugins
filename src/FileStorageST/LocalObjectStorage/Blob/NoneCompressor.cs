
namespace Neo.FileStorage.Storage.LocalObjectStorage.Blob
{
    public sealed class NoneCompressor : ICompressor
    {
        public byte[] Compress(byte[] data)
        {
            return data;
        }
        public byte[] Decompress(byte[] data)
        {
            return data;
        }

        public void Dispose() { }
    }
}
