using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Google.Protobuf;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.LocalObjectStorage.Blob;
using Neo.FileStorage.Storage.LocalObjectStorage.Blobstor;
using Neo.FileStorage.Storage.LocalObjectStorage.Metabase;
using Neo.FileStorage.Storage.Utils;
using Neo.FileStorage.Utils;
using FSAddress = Neo.FileStorage.API.Refs.Address;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Shards
{
    public class Shard : IDisposable
    {
        public const int DefaultRemoveBatchSize = 100;
        public const int DefaultRemoveInterval = 60000;
        public const bool DefaultUseWriteCache = true;
        public ShardID ID { get; private set; }
        private readonly bool useWriteCache;
        private readonly int removeBatchSize;
        private int mode;
        private readonly int removeInteral;
        private Timer timer;
        private readonly BlobStorage blobStorage;
        private readonly MB metabase;
        private readonly IActorRef workPool;
        private readonly Action<List<FSAddress>, CancellationToken> expiredTomestonesCallback;
        private readonly WriteCache writeCache;
        private CancellationTokenSource source;
        private Task prevGroup;

        public ShardMode Mode
        {
            get => (ShardMode)mode;
            set => Interlocked.Exchange(ref mode, (int)value);
        }

        public Shard(ShardSettings settings, IActorRef wp, Action<List<FSAddress>, CancellationToken> expiredCallback)
        {
            ID = new();
            useWriteCache = settings.UseWriteCache;
            blobStorage = new(settings.BlobStorageSettings);
            metabase = new(settings.MetabaseSettings.Path);
            workPool = wp;
            if (useWriteCache)
            {
                writeCache = new WriteCache(settings.WriteCacheSettings, blobStorage, metabase);
            }
            Mode = ShardMode.Undefined;
            expiredTomestonesCallback = expiredCallback;
            //GC
            removeInteral = settings.RemoverInterval <= 0 ? DefaultRemoveInterval : settings.RemoverInterval;
            removeBatchSize = settings.RemoveBatchSize <= 0 ? DefaultRemoveBatchSize : settings.RemoveBatchSize;
        }

        public void Open()
        {
            if (useWriteCache) writeCache.Open();
            blobStorage.Open();
            metabase.Open();
            timer = new(RemoveGarbage, null, removeInteral, removeInteral);
        }

        public void Dispose()
        {
            timer?.Dispose();
            source?.Cancel();
            prevGroup?.Wait();
            blobStorage?.Dispose();
            metabase?.Dispose();
        }

        public ulong ContainerSize(ContainerID cid)
        {
            return metabase.ContainerSize(cid);
        }

        public void Delete(params FSAddress[] addresses)
        {
            var smalls = new Dictionary<FSAddress, BlobovniczaID>();
            foreach (var address in addresses)
            {
                if (useWriteCache)
                {
                    writeCache.Delete(address);
                }
                var blobovniczaID = metabase.IsSmall(address);
                if (blobovniczaID.IsEmpty)
                {
                    continue;
                }
                smalls[address] = blobovniczaID;
            }

            metabase.Delete(addresses);
            foreach (var address in addresses)
            {
                if (smalls.ContainsKey(address))
                {
                    blobStorage.DeleteSmall(address, smalls[address]);
                    continue;
                }
                blobStorage.DeleteBig(address);
            }
        }


        /// <summary>
        ///  Exists checks if object is presented in shard.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool Exists(FSAddress address)
        {
            return metabase.Exists(address);
        }

        public FSObject Get(FSAddress address)
        {
            if (useWriteCache)
            {
                try
                {
                    var result = writeCache.Get(address);
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch (ObjectNotFoundException) { }
            }
            var isExist = metabase.Exists(address);
            if (!isExist)
            {
                throw new ObjectNotFoundException();
            }
            var blobovniczaID = metabase.IsSmall(address);
            if (blobovniczaID is not null)
            {
                return blobStorage.GetSmall(address, blobovniczaID);
            }
            else
            {
                return blobStorage.GetBig(address);
            }
        }

        public FSObject Head(FSAddress address, bool raw)
        {
            return metabase.Get(address, raw)?.CutPayload();
        }


        public void Inhume(FSAddress tombstone, params FSAddress[] target)
        {
            if (useWriteCache)
            {
                foreach (var address in target)
                    writeCache.Delete(address);
            }
            metabase.Inhume(tombstone, target);
        }

        public List<FSAddress> List()
        {
            var result = new List<FSAddress>();
            var containerIds = metabase.Containers();
            var filter = new SearchFilters();
            foreach (var containerId in containerIds)
            {
                var addresses = metabase.Select(containerId, filter);
                if (addresses?.Any() == true)
                {
                    result.AddRange(addresses);
                }
            }
            return result;
        }

        public List<ContainerID> ListContainers()
        {
            return metabase.Containers();
        }

        public void ToMoveIt(FSAddress address)
        {
            metabase.MoveIt(address);
        }

        public void Put(FSObject obj)
        {
            if (useWriteCache)
            {
                try
                {
                    writeCache.Put(obj);
                    return;
                }
                catch { }
            }
            var blobovniczaId = blobStorage.Put(obj);
            metabase.Put(obj, blobovniczaId);
        }

        public FSObject GetRange(FSAddress address, API.Object.Range range)
        {
            var obj = new FSObject();

            if (useWriteCache)
            {
                var result = writeCache.Get(address);
                if (result != null)
                {
                    obj.Payload = obj.Payload.Range(range.Offset, range.Offset + range.Length);
                    return obj;
                }
            }

            var isExist = metabase.Exists(address);
            if (!isExist)
            {
                return null;
            }

            var blobovniczaID = metabase.IsSmall(address);
            if (blobovniczaID != null)
            {
                var small = blobStorage.GetRangeSmall(address, range, blobovniczaID);
                if (small != null)
                {
                    obj.Payload = ByteString.CopyFrom(small);
                    return obj;
                }
            }
            else
            {
                var big = blobStorage.GetRangeBig(address, range);
                if (big != null)
                {
                    obj.Payload = ByteString.CopyFrom(big);
                    return obj;
                }
            }
            return null;
        }


        public List<FSAddress> Select(ContainerID cid, SearchFilters filter)
        {
            return metabase.Select(cid, filter);
        }

        /// <summary>
        ///  WeightValues returns current weight values of the Shard.
        /// </summary>
        /// <returns></returns>
        public ulong WeightValue()
        {
            return 0ul;
        }

        public void HandleExpiredTombstones(List<FSAddress> addresses)
        {
            List<FSAddress> inhume = new();
            metabase.IterateCoveredByTombstones(addresses.ToHashSet(), address =>
            {
                inhume.Add(address);
            });
            if (!inhume.Any()) return;
            metabase.Inhume(null, inhume.ToArray());
        }

        private void CollectExpiredObjects(ulong epoch, CancellationToken context)
        {
            List<FSAddress> expired = new();
            try
            {
                metabase.IterateExpired(epoch, (t, address) =>
                {
                    if (context.IsCancellationRequested)
                        throw new OperationCanceledException("operation is cancelled");
                    if (t != ObjectType.Tombstone)
                        expired.Add(address);
                });
            }
            catch (Exception e)
            {
                Utility.Log(nameof(Shard), LogLevel.Warning, $"iterator over expired objects failed, error: {e}");
                return;
            }
            if (!expired.Any()) return;
            if (context.IsCancellationRequested) return;
            Inhume(null, expired.ToArray());
        }

        private void CollectExpiredTombstones(ulong epoch, CancellationToken context)
        {
            List<FSAddress> expired = new();
            try
            {
                metabase.IterateExpired(epoch, (t, address) =>
                {
                    if (context.IsCancellationRequested)
                        throw new OperationCanceledException("operation is cancelled");
                    if (t == ObjectType.Tombstone)
                        expired.Add(address);
                });
            }
            catch (Exception e)
            {
                Utility.Log(nameof(Shard), LogLevel.Warning, $"iterator over expired objects failed, error: {e}");
                return;
            }
            if (!expired.Any()) return;
            if (context.IsCancellationRequested) return;
            if (expiredTomestonesCallback is not null) expiredTomestonesCallback(expired, context);
        }

        public void OnNewEpoch(ulong epoch)
        {
            source?.Cancel();
            prevGroup?.Wait();
            source = new();
            Task t1 = new(() =>
            {
                CollectExpiredObjects(epoch, source.Token);
            }, source.Token);
            Task t2 = new(() =>
            {
                CollectExpiredTombstones(epoch, source.Token);
            }, source.Token);
            workPool.Tell(new WorkerPool.NewTask
            {
                Process = "Shard.CollectExpiredObjects",
                Task = t1,
            });
            workPool.Tell(new WorkerPool.NewTask
            {
                Process = "Shard.CollectExpiredTombstones",
                Task = t2,
            });
            prevGroup = Task.WhenAll(t1, t2);
        }

        private void RemoveGarbage(object state)
        {
            List<FSAddress> buf = new();
            metabase.IterateGraveYard(g =>
            {
                if (g.GCMark)
                {
                    buf.Add(g.Address);
                }
                return removeBatchSize <= buf.Count;
            });
            if (!buf.Any()) return;
            Delete(buf.ToArray());
        }
    }
}
