using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.StorageGroup;
using Neo.FileStorage.Cache;
using Neo.FileStorage.InnerRing.Services.Audit;
using Neo.FileStorage.InnerRing.Services.Audit.Auditor;
using Neo.FileStorage.Placement;
using Neo.Wallets;
using V2Range = Neo.FileStorage.API.Object.Range;

namespace Neo.FileStorage.InnerRing
{
    public class RpcClientCache : IFSClientCache, IContainerCommunicator
    {
        public ClientCache clientCache = new();
        public Wallet wallet;

        public void Dispose()
        {
            clientCache.Dispose();
        }

        public IFSClient Get(Network.Address address)
        {
            return clientCache.Get(address);
        }

        public StorageGroup GetStorageGroup(AuditTask task, ObjectID id)
        {
            var sgAddress = new Address()
            {
                ContainerId = task.ContainerID,
                ObjectId = id
            };
            return GetStorageGroup(task.Cancellation, sgAddress, task.Netmap, task.ContainerNodes);
        }
        public StorageGroup GetStorageGroup(CancellationToken cancellation, Address sgAddress, NetMap netMap, List<List<Node>> containerNodes)
        {
            List<List<Node>> nodes;
            try
            {
                nodes = NetworkMapBuilder.BuildObjectPlacement(netMap, containerNodes, sgAddress.ObjectId);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("can't build object placement: {0}", e.Message));
            }
            foreach (var node in nodes.Flatten())
            {
                Network.Address addr;
                try
                {
                    addr = Network.Address.FromString(node.NetworkAddress);
                }
                catch (Exception e)
                {
                    Utility.Log("RpcClientCache", LogLevel.Warning, string.Format("can't parse remote address,address:{0},errot:{1}", node.NetworkAddress, e.Message));
                    continue;
                }
                IFSClient cli;
                try
                {
                    cli = Get(addr);
                }
                catch (Exception e)
                {
                    Utility.Log("RpcClientCache", LogLevel.Warning, string.Format("can't setup remote connection,address:{0},errot:{1}", addr, e.Message));
                    continue;
                }
                API.Object.Object obj;
                try
                {
                    var source = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                    source.CancelAfter(TimeSpan.FromMinutes(1));
                    var key = wallet.GetAccounts().ToArray()[0].GetKey().Export().LoadWif();
                    obj = cli.GetObject(sgAddress, false, new CallOptions() { Key = key }, context: source.Token).Result;
                }
                catch (Exception e)
                {
                    Utility.Log("RpcClientCache", LogLevel.Warning, string.Format("can't get storage group object,error:{0}", e.Message));
                    continue;
                }
                StorageGroup sg;
                try
                {
                    sg = StorageGroup.Parser.ParseFrom(obj.Payload);
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("can't parse storage group payload: {0}", e.Message));
                }
                return sg;
            }
            throw new Exception("object not found");
        }

        public API.Object.Object GetHeader(AuditTask task, Node node, ObjectID id, bool relay)
        {
            bool raw = !relay;
            uint ttl = relay ? 10u : 1u;
            Address objAddress = new()
            {
                ContainerId = task.ContainerID,
                ObjectId = id
            };
            Network.Address addr;
            try
            {
                addr = Network.Address.FromString(node.NetworkAddress);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("can't parse remote address {0}: {1}", node.NetworkAddress, e.Message));
            }
            IFSClient client;
            try
            {
                client = Get(addr);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("can't setup remote connection with {0}: {1}", addr, e.Message));
            }
            API.Object.Object head;
            try
            {
                var source = CancellationTokenSource.CreateLinkedTokenSource(task.Cancellation);
                source.CancelAfter(TimeSpan.FromMinutes(1));
                var key = wallet.GetAccounts().ToArray()[0].GetKey().Export().LoadWif();
                head = client.GetObjectHeader(objAddress, raw, options: new CallOptions() { Key = key }, context: source.Token).Result;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("object head error: {0}", e.Message));
            }
            return head;
        }

        public byte[] GetRangeHash(AuditTask task, Node node, ObjectID id, Neo.FileStorage.API.Object.Range rng)
        {
            Address objAddress = new()
            {
                ContainerId = task.ContainerID,
                ObjectId = id
            };
            Network.Address addr;
            try
            {
                addr = Network.Address.FromString(node.NetworkAddress);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("can't parse remote address,address:{0},error:{1}", node.NetworkAddress, e.Message));
            }
            IFSClient cli;
            try
            {
                cli = Get(addr);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("can't setup remote connection with {0}:{1}", node.NetworkAddress, e.Message));
            }
            List<byte[]> result;
            try
            {
                var source = new CancellationTokenSource();
                source.CancelAfter(TimeSpan.FromMinutes(1));
                var key = wallet.GetAccounts().ToArray()[0].GetKey().Export().LoadWif();
                result = cli.GetObjectPayloadRangeHash(objAddress, new List<V2Range> { rng }, ChecksumType.Tz, null, new() { Ttl = 1, Key = key }, source.Token).Result;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("object rangehash error :{0}", e.Message));
            }
            return result[0];
        }
    }
}
