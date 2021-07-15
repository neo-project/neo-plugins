using System;
using Neo.IO.Data.LevelDB;

namespace Neo.FileStorage.Database.LevelDB
{
    public sealed class DB : IDB
    {
        private readonly IO.Data.LevelDB.DB db;

        public DB(string path)
        {
            db = IO.Data.LevelDB.DB.Open(System.IO.Path.GetFullPath(path), new Options() { CreateIfMissing = true, FilterPolicy = Native.leveldb_filterpolicy_create_bloom(15) });
        }

        public void Dispose()
        {
            db?.Dispose();
        }

        public byte[] Get(byte[] key)
        {
            return db.Get(ReadOptions.Default, key);
        }

        public void Put(byte[] key, byte[] value)
        {
            db.Put(WriteOptions.Default, key, value);
        }

        public void Delete(byte[] key)
        {
            db.Delete(WriteOptions.Default, key);
        }

        public bool Contains(byte[] key)
        {
            return db.Contains(ReadOptions.Default, key);
        }

        public void Iterate(byte[] prefix, Func<byte[], byte[], bool> handler)
        {
            using Iterator it = db.NewIterator(ReadOptions.Default);
            for (it.Seek(prefix); it.Valid(); it.Next())
            {
                if (!it.Key().AsSpan().StartsWith(prefix)) break;
                if (handler(it.Key(), it.Value())) break;
            }
        }
    }
}
