using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Akka.Util.Extensions;
using Google.Protobuf;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.LocalObjectStorage.Shards;
using Neo.FileStorage.Storage.Services.Object.Acl.EAcl;
using Neo.FileStorage.Storage.Services.Object.Search;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Engine
{
    public sealed class StorageEngine : ILocalHeadSource, ILocalSearchSource, IDisposable
    {
        private readonly Dictionary<ShardID, Shard> shards = new();
        private readonly ReaderWriterLockSlim mtx = new();

        public void Open()
        {
            foreach (var shard in shards.Values)
                shard.Open();
        }

        public void Dispose()
        {
            foreach (var shard in shards.Values)
                shard.Dispose();
            shards.Clear();
            mtx.Dispose();
        }

        public FSObject Get(Address address)
        {
            SplitInfo spi = null;
            foreach (var shard in SortedShards(address))
            {
                try
                {
                    return shard.Get(address);
                }
                catch (ObjectNotFoundException)
                {
                    continue;
                }
                catch (SplitInfoException e)
                {
                    if (spi is null)
                    {
                        spi = new();
                    }
                    Helper.MergeSplitInfo(e.SplitInfo, spi);
                    if (spi.Link is not null && spi.LastPart is not null)
                        throw new SplitInfoException(spi);
                    continue;
                }
            }
            if (spi is not null) throw new SplitInfoException(spi);
            throw new ObjectNotFoundException();
        }

        public FSObject GetRange(Address address, API.Object.Range range)
        {
            SplitInfo spi = null;
            foreach (var shard in SortedShards(address))
            {
                try
                {
                    return shard.GetRange(address, range);
                }
                catch (ObjectNotFoundException)
                {
                    continue;
                }
                catch (SplitInfoException e)
                {
                    if (spi is null)
                    {
                        spi = new();
                    }
                    Helper.MergeSplitInfo(e.SplitInfo, spi);
                    if (spi.Link is not null && spi.LastPart is not null)
                        throw new SplitInfoException(spi);
                    continue;
                }
            }
            if (spi is not null) throw new SplitInfoException(spi);
            throw new ObjectNotFoundException();
        }

        public void Put(FSObject obj)
        {
            foreach (var shard in SortedShards(obj.Address))
            {
                var isExist = shard.Exists(obj.Address);
                if (isExist)
                {
                    shard.ToMoveIt(obj.Address);
                    return;
                }
                shard.Put(obj);
                return;
            }
        }

        public void Delete(params Address[] addresses)
        {
            foreach (var address in addresses)
            {
                foreach (var shard in SortedShards(address))
                {
                    if (shard.Exists(address))
                    {
                        shard.Inhume(null, address);
                        return;
                    }
                }
            }
        }

        public List<Address> Select(ContainerID cid, SearchFilters filters)
        {
            var result = new HashSet<Address>();
            foreach (var shard in UnsortedShards())
            {
                var addresses = shard.Select(cid, filters);
                if (addresses?.Any() == true)
                {
                    addresses.ForEach(a => result.Add(a));
                }
            }
            return result.ToList();
        }

        public List<Address> List(ulong limit)
        {
            var result = new HashSet<Address>();
            foreach (var shard in UnsortedShards())
            {
                var addresses = shard.List();
                if (addresses is null) { continue; }
                foreach (var address in addresses)
                {
                    if (!result.Contains(address))
                    {
                        result.Add(address);
                        limit--;
                        if (limit == 0)
                        {
                            return result.ToList();
                        }
                    }
                }
            }
            return result.ToList();
        }

        public ulong ContainerSize(ContainerID id)
        {
            ulong total = 0;
            foreach (var shard in UnsortedShards())
            {
                var size = shard.ContainerSize(id);
                total += size;
            }
            return total;
        }

        public List<ContainerID> ListContainers()
        {
            var uniqueIDs = new Dictionary<string, ContainerID>();
            foreach (var shard in UnsortedShards())
            {
                var containerIds = shard.ListContainers();
                if (containerIds?.Any() == true)
                {
                    foreach (var containerId in containerIds)
                    {
                        uniqueIDs[containerId.ToString()] = containerId;
                    }
                }
            }

            return uniqueIDs.Values.ToList();
        }

        public bool Exist(Address address)
        {
            foreach (var shard in SortedShards(address))
            {
                var isExist = shard.Exists(address);
                if (isExist)
                {
                    return true;
                }
            }
            return false;
        }

        public FSObject Head(Address address)
        {
            return Head(address, false);
        }

        public FSObject Head(Address address, bool raw)
        {
            SplitInfo spi = null;
            foreach (var shard in SortedShards(address))
            {
                try
                {
                    return shard.Head(address, raw);
                }
                catch (ObjectNotFoundException)
                {
                    continue;
                }
                catch (SplitInfoException e)
                {
                    if (spi is null)
                    {
                        spi = new();
                    }
                    Helper.MergeSplitInfo(e.SplitInfo, spi);
                    if (spi.Link is not null && spi.LastPart is not null)
                        throw new SplitInfoException(spi);
                    continue;
                }
            }
            if (spi is not null) throw new SplitInfoException(spi);
            throw new ObjectNotFoundException();
        }

        public void Inhume(Address tombstone, params Address[] addresses)
        {
            foreach (var address in addresses)
            {
                foreach (var shard in SortedShards(address))
                {
                    shard.Inhume(tombstone, address);
                }
            }
        }

        private void Inhume(Address tombstone, Address[] addresses, bool check_exists)
        {
            bool root = false;
            bool RootCheck()
            {
                return check_exists && root;
            }
            foreach (var shard in SortedShards(addresses[0]))
            {
                if (check_exists)
                {
                    try
                    {
                        if (!shard.Exists(addresses[0]) & RootCheck())
                            continue;
                    }
                    catch (ObjectAlreadyRemovedException)
                    {
                        break;
                    }
                    catch (SplitInfoException)
                    {
                        root = true;
                        continue;
                    }
                }
                try
                {
                    shard.Inhume(tombstone, addresses);
                    if (!RootCheck()) break;
                }
                catch
                {
                    continue;
                }
            }
        }

        public void AddShard(Shard shard)
        {
            shards[shard.ID] = shard;
        }

        public void ProcessExpiredTomstones(List<Address> addresses, CancellationToken token)
        {
            foreach (var shard in UnsortedShards())
            {
                shard.HandleExpiredTombstones(addresses);
                if (token.IsCancellationRequested) return;
            }
        }

        private List<Shard> UnsortedShards()
        {
            try
            {
                mtx.EnterReadLock();
                return shards.Values.ToList();
            }
            finally
            {
                mtx.ExitReadLock();
            }
        }

        /// <summary>
        /// sort by value-distance * weights
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private List<Shard> SortedShards(Address address)
        {
            try
            {
                mtx.EnterReadLock();
                var target = address.ToByteArray().Murmur64(0);
                var list = shards.Values.Select(s => new ShardDistance
                {
                    Shard = s,
                    Weight = s.WeightValue(),
                    Distance = Utility.StrictUTF8.GetBytes(s.ID.ToString()).Murmur64(0).Distance(target),
                });
                return list.OrderBy(s => s.Sort).Select(s => s.Shard).ToList();
            }
            finally
            {
                mtx.ExitReadLock();
            }
        }

        private class ShardDistance
        {
            public Shard Shard;
            public ulong Weight;
            public ulong Distance;
            public ulong Sort;

            public ShardDistance SetSort()
            {
                Sort = Weight * Distance;
                return this;
            }
        }
    }
}
