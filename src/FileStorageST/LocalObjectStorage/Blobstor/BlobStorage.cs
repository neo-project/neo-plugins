using System;
using Google.Protobuf;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.LocalObjectStorage.Blob;
using FSObject = Neo.FileStorage.API.Object.Object;
using FSRange = Neo.FileStorage.API.Object.Range;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Blobstor
{
    public sealed class BlobStorage : IDisposable
    {
        public const ulong DefaultSmallSizeLimit = 1 << 20;
        public const string BlobovniczasDir = "blobovnicza";
        private readonly ICompressor compressor;
        private readonly FSTree fsTree;
        private readonly BlobovniczaTree blobovniczas;
        public ulong SmallSizeLimit { get; init; }

        public BlobStorage(BlobStorageSettings settings)
        {
            SmallSizeLimit = settings.SmallSizeLimit;
            compressor = settings.Compress ? new ZstdCompressor() : null;
            fsTree = new()
            {
                RootPath = settings.Path,
                Depth = settings.ShallowDepth,
            };
            blobovniczas = new()
            {
                BlzRootPath = System.IO.Path.Join(settings.Path, BlobovniczasDir),
                Compressor = compressor,
                BlzShallowDepth = (ulong)settings.BlobovniczasSettings.ShallowDepth,
                BlzShallowWidth = (ulong)settings.BlobovniczasSettings.ShallowWidth
            };
        }

        public void Open()
        {
            blobovniczas.Open();
        }

        public void Dispose()
        {
            blobovniczas?.Dispose();
        }

        private bool IsBig(byte[] data)
        {
            return (ulong)data.Length > SmallSizeLimit;
        }

        private bool ExistsBig(Address address)
        {
            try
            {
                _ = fsTree.Exists(address);
                return true;
            }
            catch (ObjectFileNotFoundException)
            {
                return false;
            }
        }

        private bool ExistsSmall(Address address)
        {
            throw new NotImplementedException("neofs-node not implemented");
        }

        public bool Exists(Address address)
        {
            if (ExistsBig(address)) return true;
            return ExistsSmall(address);
        }

        public FSObject GetSmall(Address address, BlobovniczaID id = null)
        {
            return blobovniczas.Get(address, id);
        }

        public FSObject GetBig(Address address)
        {
            byte[] data = fsTree.Get(address);
            data = compressor.Decompress(data);
            return FSObject.Parser.ParseFrom(data);
        }

        public byte[] GetRangeSmall(Address address, FSRange range, BlobovniczaID id = null)
        {
            return blobovniczas.GetRange(address, range, id);
        }

        public byte[] GetRangeBig(Address address, FSRange range)
        {
            FSObject obj = GetBig(address);
            if (obj.PayloadSize < range.Offset + range.Length)
                throw new RangeOutOfBoundsException();
            int start = (int)range.Offset;
            int end = start + (int)range.Length;
            return obj.Payload.ToByteArray()[start..end];
        }

        public BlobovniczaID Put(FSObject obj)
        {
            byte[] data = obj.ToByteArray();
            bool big = IsBig(data);
            if (big)
            {
                data = compressor.Compress(data);
                fsTree.Put(obj.Address, data);
                return null;
            }
            return blobovniczas.Put(obj);
        }

        public void DeleteBig(Address address)
        {
            fsTree.Delete(address);
        }

        public void DeleteSmall(Address address, BlobovniczaID id = null)
        {
            blobovniczas.Delete(address, id);
        }

        public void Iterate()
        {
            throw new NotImplementedException("neofs-node not implemented");
        }
    }
}
