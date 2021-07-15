using System;

namespace Neo.FileStorage.Database
{
    public interface IDB : IDisposable
    {
        byte[] Get(byte[] key);
        void Put(byte[] key, byte[] value);
        void Delete(byte[] key);
        bool Contains(byte[] key);
        void Iterate(byte[] prefix, Func<byte[], byte[], bool> handler);
    }
}
