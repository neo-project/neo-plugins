using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Cache;
using Neo.FileStorage.Storage.LocalObjectStorage.Blob;
using static Neo.Utility;
using FSObject = Neo.FileStorage.API.Object.Object;
using FSRange = Neo.FileStorage.API.Object.Range;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Blobstor
{
    public class BlobovniczaTree : IDisposable
    {
        public const int DefaultOpenedCacheSize = 16;
        public const int DefaultBlzShallowDepth = 4;
        public const int DefaultBlzShallowWidth = 4;

        public ulong BlzShallowDepth { get; init; }
        public ulong BlzShallowWidth { get; init; }
        public ulong SmallSizeLimit { get; init; }
        public ulong FullSizeLimit { get; init; }
        public ICompressor Compressor { get; init; }
        public string BlzRootPath { get; init; }
        private readonly int cacheSize;
        private readonly object openedLock = new();
        private readonly object activeLock = new();
        private readonly LRUCache<string, Blobovnicza> opened;
        private readonly ConcurrentDictionary<string, BlobovniczaWithIndex> active = new();

        public BlobovniczaTree(int cap = 0)
        {
            BlzShallowDepth = DefaultBlzShallowDepth;
            BlzShallowWidth = DefaultBlzShallowWidth;
            SmallSizeLimit = Blobovnicza.DefaultObjSizeLimit;
            FullSizeLimit = Blobovnicza.DefaultFullSizeLimit;
            cacheSize = cap == 0 ? DefaultOpenedCacheSize : cap;
            opened = new(cacheSize, OnEvicted);
        }

        private void OnEvicted(string key, Blobovnicza value)
        {
            if (active.ContainsKey(Path.GetFullPath(key)))
                return;
            value.Dispose();
        }

        public void Open()
        {
            bool DoInit(string path)
            {
                try
                {
                    Blobovnicza b = OpenBlobovnicza(path);
                }
                catch (Exception)
                {
                    Log(nameof(BlobovniczaTree), LogLevel.Debug, $"could not open blobovnicza {path}");
                    throw;
                }
                Log(nameof(BlobovniczaTree), LogLevel.Debug, $"blobovnicza opened {path}");
                return false;
            };
            IterateLeaves(null, DoInit);
        }

        public BlobovniczaID Put(FSObject obj)
        {
            BlobovniczaID id = null;
            bool DoPut(string p)
            {
                BlobovniczaWithIndex bi = GetActived(p);
                try
                {
                    bi.Blobovnicza.Put(obj);
                }
                catch (BlobFullException)
                {
                    Log(nameof(BlobovniczaTree), LogLevel.Debug, "blobovnicza overflowed, " + Path.Join(p, BitConverter.GetBytes(bi.Index).ToHexString()));
                    UpdateActive(p, bi.Index);
                    return DoPut(p);
                }
                id = Path.Join(p, BitConverter.GetBytes(bi.Index).ToHexString());
                Log(nameof(BlobovniczaTree), LogLevel.Debug, "object successfully saved in active blobovnicza, path:" + p + " addr:" + obj.Address.String());
                return true;
            };
            IterateDeepest(obj.Address, DoPut);
            if (id is null) throw new InvalidOperationException("could not save the object in any blobovnicza");
            return id;
        }

        public FSObject Get(Address address, BlobovniczaID id = null)
        {
            if (id is not null)
            {
                var blz = OpenBlobovnicza(id);
                return blz.Get(address);
            }
            HashSet<string> cache = new();
            FSObject obj = null;
            bool DoGet(string path)
            {
                var dir = Path.GetDirectoryName(path);
                try
                {
                    cache.Add(dir);
                    return OperateFromLevel(path, !cache.Contains(dir), blz =>
                    {
                        try
                        {
                            obj = blz.Get(address);
                        }
                        catch (Exception e) when (e is not ObjectNotFoundException)
                        {
                            Log(nameof(BlobovniczaTree), LogLevel.Debug, "could not read object from active blobovnicza");
                            return false;
                        }
                        return true;
                    });
                }
                catch (ObjectNotFoundException)
                {
                    Log(nameof(BlobovniczaTree), LogLevel.Debug, $"could not get object from level, level={path}");
                    return false;
                }
                catch (Exception e)
                {
                    Log(nameof(BlobovniczaTree), LogLevel.Debug, $"could not get object from level, level={path}, error={e.Message}");
                    throw;
                }
            };
            IterateLeaves(address, DoGet);
            if (obj is null) throw new ObjectNotFoundException();
            return obj;
        }

        public void Delete(Address address, BlobovniczaID id = null)
        {
            if (id is not null)
            {
                Blobovnicza b = OpenBlobovnicza(id.ToString());
                b.Delete(address);
                return;
            }
            HashSet<string> cache = new();
            bool DoDelete(string path)
            {
                var dir = Path.GetDirectoryName(path);
                try
                {
                    cache.Add(dir);
                    return OperateFromLevel(path, !cache.Contains(dir), blz =>
                    {
                        try
                        {
                            blz.Delete(address);
                        }
                        catch (Exception e) when (e is not ObjectNotFoundException)
                        {
                            Log(nameof(BlobovniczaTree), LogLevel.Debug, "could not read object from active blobovnicza");
                            return false;
                        }
                        return true;
                    });
                }
                catch (ObjectNotFoundException)
                {
                    Log(nameof(BlobovniczaTree), LogLevel.Debug, $"could not get object from level, level={path}");
                    return false;
                }
                catch (Exception e)
                {
                    Log(nameof(BlobovniczaTree), LogLevel.Debug, $"could not get object from level, level={path}, error={e.Message}");
                    throw;
                }
            };
            if (!IterateLeaves(address, DoDelete))
                throw new ObjectNotFoundException();
        }

        public byte[] GetRange(Address address, FSRange range, BlobovniczaID id = null)
        {
            if (id is not null)
            {
                var blz = OpenBlobovnicza(id.ToString());
                return blz.GetRange(address, range);
            }
            HashSet<string> cache = new();
            byte[] data = null;
            bool DoGetRange(string path)
            {
                var dir = Path.GetDirectoryName(path);
                try
                {
                    return OperateFromLevel(path, cache.Contains(dir), blz =>
                    {
                        try
                        {
                            data = blz.GetRange(address, range);
                        }
                        catch (Exception e) when (e is not ObjectNotFoundException)
                        {
                            Log(nameof(BlobovniczaTree), LogLevel.Debug, "could not read object from active blobovnicza");
                            return false;
                        }
                        return true;
                    });
                }
                catch (ObjectNotFoundException)
                {
                    Log(nameof(BlobovniczaTree), LogLevel.Debug, $"could not get object from level, level={path}");
                    return false;
                }
                catch (Exception e)
                {
                    Log(nameof(BlobovniczaTree), LogLevel.Debug, $"could not get object from level, level={path}, error={e.Message}");
                }
                cache.Add(dir);
                return false;
            };
            IterateLeaves(address, DoGetRange);
            if (data is null) throw new ObjectNotFoundException();
            return data;
        }

        private bool OperateFromLevel(string path, bool try_active, Func<Blobovnicza, bool> func)
        {
            var level_path = Path.GetDirectoryName(path);
            if (opened.TryGet(path, out Blobovnicza blz))
            {
                if (func(blz)) return true;
            }
            if (active.TryGetValue(level_path, out BlobovniczaWithIndex bi) && try_active)
            {
                if (func(bi.Blobovnicza)) return true;
            }
            ulong index = BitConverter.ToUInt64(Path.GetFileName(path).HexToBytes());
            if (bi.Index < index) throw new ObjectNotFoundException();
            Blobovnicza b = OpenBlobovnicza(path);
            if (func(b)) return true;
            return false;
        }

        private BlobovniczaWithIndex GetActived(string path)
        {
            return UpdateAndGet(path, null);
        }

        private void UpdateActive(string path, ulong old)
        {
            Log(nameof(BlobovniczaTree), LogLevel.Debug, "updating active blobovnicza...");
            UpdateAndGet(path, old);
            Log(nameof(BlobovniczaTree), LogLevel.Debug, "active blobovnicza successfully updated");
        }

        private BlobovniczaWithIndex UpdateAndGet(string path, ulong? old)
        {
            lock (activeLock)
            {
                bool exist = active.TryGetValue(path, out BlobovniczaWithIndex bi);
                if (exist)
                {
                    if (old is null) return bi;
                    if (bi.Index == BlzShallowWidth - 1)
                        throw new InvalidOperationException("no more blobovniczas");
                    if (bi.Index != old)
                        return bi;
                    bi.Index++;
                }
                else
                {
                    bi = new();
                }
                bi.Blobovnicza = OpenBlobovnicza(Path.Join(path, BitConverter.GetBytes(bi.Index).ToHexString()));
                if (active.TryGetValue(path, out BlobovniczaWithIndex b) && bi.Blobovnicza.Equals(b.Blobovnicza)) return b;
                active[path] = bi;
                opened.Remove(path);
                return bi;
            }
        }

        private bool IterateLeaves(Address address, Func<string, bool> func)
        {
            return IterateSorted(address, new List<string>(), BlzShallowDepth, paths => func(Path.Join(paths.ToArray())));
        }

        private void IterateDeepest(Address address, Func<string, bool> func)
        {
            ulong depth = BlzShallowDepth;
            if (0 < depth) depth--;
            IterateSorted(address, new List<string>(), depth, paths => func(Path.Join(paths.ToArray())));
        }

        public bool IterateSorted(Address address, List<string> current_path, ulong depth, Func<List<string>, bool> func)
        {
            List<ulong> indices = IndexSlice(BlzShallowWidth);
            ulong hash = AddressHash(address, Path.Join(current_path.ToArray()));
            List<ulong> sorted = indices.OrderBy(p => p.Distance(hash)).ToList();
            bool exec = (ulong)current_path.Count == depth;
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i == 0)
                    current_path.Add(BitConverter.GetBytes(sorted[i]).ToHexString());
                else
                    current_path[^1] = BitConverter.GetBytes(sorted[i]).ToHexString();
                if (exec)
                {
                    if (func(current_path)) return true;
                }
                else
                {
                    if (IterateSorted(address, current_path.ToList(), depth, func)) return true;
                }
            }
            return false;
        }

        private List<ulong> IndexSlice(ulong number)
        {
            List<ulong> s = new();
            for (ulong i = 0; i < number; i++)
                s.Add(i);
            return s;
        }

        private ulong AddressHash(Address address, string path)
        {
            return StrictUTF8.GetBytes(address?.String() + path).Murmur64(0);
        }


        private Blobovnicza OpenBlobovnicza(string path)
        {
            lock (openedLock)
            {
                if (opened.TryGet(path, out Blobovnicza b))
                    return b;
                b = new Blobovnicza(Path.Join(BlzRootPath, path), Compressor)
                {
                    FullSizeLimit = FullSizeLimit,
                    ObjSizeLimit = SmallSizeLimit,
                };
                b.Open();
                opened.Add(path, b);
                return b;
            }
        }

        public void Dispose()
        {
            lock (active)
            {
                foreach (var b in active.Values)
                    b.Blobovnicza.Dispose();
                active.Clear();
            }
            lock (opened)
            {
                opened.Purge();
            }
        }
    }
}
