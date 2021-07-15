using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using Google.Protobuf;
using Neo.FileStorage.API.Audit;
using Neo.FileStorage.API.Container;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.StorageGroup;
using Neo.FileStorage.InnerRing.Processors;
using Neo.FileStorage.Morph.Invoker;
using Neo.IO;
using NodeInfo = Neo.FileStorage.InnerRing.Processors.NodeInfo;

namespace Neo.FileStorage.InnerRing
{
    public abstract class SettlementDeps
    {
        public MorphInvoker Invoker;
        public RpcClientCache clientCache;

        public List<DataAuditResult> AuditResultsForEpoch(ulong epoch)
        {
            List<byte[]> idList = Invoker.ListAuditResultsByEpoch((long)epoch);
            var res = new List<DataAuditResult>();
            foreach (var id in idList)
            {
                DataAuditResult dataAuditResult = Invoker.GetAuditResult(id);
                res.Add(dataAuditResult);
            }
            return res;
        }

        public Container ContainerInfo(ContainerID cid)
        {
            return Invoker.GetContainer(cid)?.Container;
        }

        public void BuildContainer(ulong epoch, ContainerID cid, out List<List<Node>> containerNodes, out NetMap netMap)
        {
            if (epoch > 0)
                netMap = Invoker.EpochSnapshot(epoch);
            else
                netMap = Invoker.Snapshot(0);
            Container cnr = Invoker.GetContainer(cid)?.Container;
            containerNodes = netMap.GetContainerNodes(cnr.PlacementPolicy, cid.Value.ToByteArray());
        }

        public NodeInfo[] ContainerNodes(ulong epoch, ContainerID cid)
        {
            BuildContainer(epoch, cid, out List<List<Node>> cn, out NetMap netMap);
            List<Node> ns = cn.Flatten();
            List<NodeInfo> res = new List<NodeInfo>();
            foreach (var node in ns)
                res.Add(new NormalNodeInfoWrapper(node));
            return res.ToArray();
        }

        public StorageGroup SGInfo(Address address)
        {
            BuildContainer(0, address.ContainerId, out var cn, out var nm);
            return clientCache.GetStorageGroup(new CancellationToken(), address, nm, cn);
        }

        public OwnerID ResolveKey(NodeInfo nodeInfo)
        {
            return nodeInfo.PublicKey().PublicKeyToOwnerID();
        }

        public void transfer(OwnerID sender, OwnerID recipient, long amount, byte[] details)
        {
            Utility.Log("SettlementDeps", LogLevel.Info, string.Format("sender:{0},recipient:{1},amount (GASe-12):{2},details:{3}", sender, recipient, amount, Encoding.UTF8.GetString(details)));
            //notary
            var from = new UInt160(Cryptography.Base58.Base58CheckDecode(Cryptography.Base58.Encode(sender.Value.ToByteArray())).Skip(1).ToArray());
            var to = new UInt160(Cryptography.Base58.Base58CheckDecode(Cryptography.Base58.Encode(recipient.Value.ToByteArray())).Skip(1).ToArray());
            Invoker.TransferX(from.ToArray(), to.ToArray(), amount, details);
            Utility.Log("SettlementDeps", LogLevel.Info, "transfer transaction for audit was successfully sent");
        }

        public abstract void Transfer(OwnerID sender, OwnerID recipient, long amount, byte[] details);
    }

    public class AuditSettlementDeps : SettlementDeps
    {
        public override void Transfer(OwnerID sender, OwnerID recipient, long amount, byte[] details)
        {
            transfer(sender, recipient, amount, System.Text.Encoding.UTF8.GetBytes("settlement-audit"));
        }
    };
    public class BasicIncomeSettlementDeps : SettlementDeps
    {
        public ulong BasicRate => Settings.Default.BasicIncomeRate;
        public override void Transfer(OwnerID sender, OwnerID recipient, long amount, byte[] details)
        {
            transfer(sender, recipient, amount, System.Text.Encoding.UTF8.GetBytes("settlement-basic-income"));
        }

        public BigInteger Balance(OwnerID id)
        {
            return Invoker.BalanceOf(id.ToByteArray());
        }

        public Estimations[] Estimations(ulong epoch)
        {
            List<byte[]> estimationIDs = Invoker.ListSizes(epoch);
            List<Estimations> result = new List<Estimations>();
            foreach (var estimationID in estimationIDs)
            {
                try
                {
                    Estimations estimation = Invoker.InvokeGetContainerSize(ContainerID.Parser.ParseFrom(estimationID));
                    result.Add(estimation);
                }
                catch (Exception e)
                {
                    Utility.Log("BasicIncomeSettlementDeps", LogLevel.Warning, string.Format("can't get used space estimation,estimation_id:{0},error:{1}", estimationID.ToHexString(), e.Message));
                }
            }
            return result.ToArray();
        }
    };
}
