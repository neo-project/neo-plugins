using System;
using System.IO;
using System.Text;
using System.Threading;
using Google.Protobuf;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Database;
using Neo.FileStorage.Database.LevelDB;
using FSObject = Neo.FileStorage.API.Object.Object;
using FSRange = Neo.FileStorage.API.Object.Range;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Blob
{
    public sealed class Blobovnicza : IEquatable<Blobovnicza>, IDisposable
    {
        public const ulong DefaultFullSizeLimit = 1 << 30;
        public const ulong DefaultObjSizeLimit = 1 << 20;
        public readonly Func<byte[], byte[]> DefaultCompressor = d => d;
        public string Path { get; private set; }
        public ICompressor Compressor { get; init; }
        public ulong FullSizeLimit { get; init; }
        public ulong ObjSizeLimit { get; init; }
        private IDB dB;
        private long filled;

        public Blobovnicza(string path, ICompressor compressor = null)
        {
            Path = path;
            FullSizeLimit = DefaultFullSizeLimit;
            ObjSizeLimit = DefaultObjSizeLimit;
            Compressor = compressor ?? new NoneCompressor();
        }

        public void Open()
        {
            var full = System.IO.Path.GetFullPath(Path);
            if (!Directory.Exists(full))
                Directory.CreateDirectory(full);
            dB = new DB(full);
        }

        public void Dispose()
        {
            dB?.Dispose();
        }

        private byte[] Addresskey(Address address)
        {
            return Encoding.UTF8.GetBytes(address.String());
        }

        public FSObject Get(Address address)
        {
            if (address is null)
                throw new ArgumentNullException(nameof(address));
            var raw = dB.Get(Addresskey(address));
            if (raw is null) throw new ObjectNotFoundException();
            return FSObject.Parser.ParseFrom(Compressor.Decompress(raw));
        }

        public byte[] GetRange(Address address, FSRange range)
        {
            var obj = Get(address);
            var from = (int)range.Offset;
            var to = from + (int)range.Length;
            if (to < from)
                throw new ArgumentException("invalid range");
            else if (obj.Payload.Length < to)
                throw new RangeOutOfBoundsException();
            return obj.Payload.ToByteArray()[from..to];
        }

        public void Put(FSObject obj)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));
            if (FullSizeLimit < (ulong)filled) throw new BlobFullException();
            var raw = Compressor.Compress(obj.ToByteArray());
            if (ObjSizeLimit < (ulong)raw.Length)
                throw new ObjectSizeExceedLimitException();
            var key = Addresskey(obj.Address);
            dB.Put(key, raw);
            IncSize(raw.Length);
        }

        public void Delete(Address address)
        {
            if (address is null)
                throw new ArgumentNullException(nameof(address));
            var raw = dB.Get(Addresskey(address));
            if (raw is null) throw new ObjectNotFoundException();
            dB.Delete(Addresskey(address));
            DecSize(raw.Length);
        }

        private void IncSize(long size)
        {
            Interlocked.Add(ref filled, size);
        }

        private void DecSize(long size)
        {
            Interlocked.Add(ref filled, -size);
        }

        public bool Equals(Blobovnicza other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Path == other.Path) return true;
            return false;
        }
    }
}
