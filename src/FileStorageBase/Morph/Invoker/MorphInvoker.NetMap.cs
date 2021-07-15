using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Neo.Cryptography.ECC;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Morph.Invoker;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using static Neo.FileStorage.API.Netmap.Helper;
using Array = Neo.VM.Types.Array;

namespace Neo.FileStorage.Morph.Invoker
{
    public partial class MorphInvoker
    {
        private const string AddPeerMethod = "addPeer";
        private const string NewEpochMethod = "newEpoch";
        private const string UpdateStateMethod = "updateState";
        private const string EpochMethod = "epoch";
        private const string SnapshotMethod = "snapshot";
        private const string NetMapMethod = "netmap";
        private const string EpochSnapshotMethod = "snapshotByEpoch";
        private const string SetConfigMethod = "setConfig";
        private const string SetInnerRingMethod = "updateInnerRing";

        private const long ExtraFee = 0;

        public bool AddPeer(NodeInfo info)
        {
            return Invoke(out _, NetMapContractHash, AddPeerMethod, SideChainFee, info.ToByteArray());
        }

        public ulong Epoch()
        {
            InvokeResult result = TestInvoke(NetMapContractHash, EpochMethod);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (Epoch)");
            return (ulong)result.ResultStack[0].GetInteger();
        }

        public bool NewEpoch(ulong epochNumber)
        {
            return Invoke(out _, NetMapContractHash, NewEpochMethod, SideChainFee, epochNumber);
        }

        public bool UpdatePeerState(NodeInfo.Types.State state, byte[] key)
        {
            return Invoke(out _, NetMapContractHash, UpdateStateMethod, SideChainFee, (int)state, key);
        }

        public NodeInfo[] NetMap()
        {
            InvokeResult result = TestInvoke(NetMapContractHash, NetMapMethod);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (NetMap)");
            if (result.ResultStack.Length != 1) throw new Exception(string.Format("unexpected stack item count ({0})", result.ResultStack.Length));
            if (result.ResultStack[0] is VM.Types.Null) return System.Array.Empty<NodeInfo>();
            Array peers = (Array)result.ResultStack[0];
            IEnumerator<StackItem> peersEnumerator = peers.GetEnumerator();
            List<byte[]> res = new();
            while (peersEnumerator.MoveNext())
            {
                Array peer = (Array)peersEnumerator.Current;
                if (peer.Count != 1) throw new Exception(string.Format("unexpected stack item count (PeerInfo): expected {0}, has {1}", 1, peer.Count));
                IEnumerator<StackItem> peerEnumerator = peer.GetEnumerator();
                while (peerEnumerator.MoveNext())
                {
                    res.Add(peerEnumerator.Current.GetSpan().ToArray());
                }
            }

            return res.Select(p => NodeInfo.Parser.ParseFrom(p)).ToArray();
        }

        public NetMap Snapshot(int different)
        {
            InvokeResult result = TestInvoke(NetMapContractHash, SnapshotMethod, different);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (Snapshot)");
            if (result.ResultStack.Length != 1) throw new Exception(string.Format("unexpected stack item count ({0})", result.ResultStack.Length));
            Array peers = (Array)result.ResultStack[0];
            IEnumerator<StackItem> peersEnumerator = peers.GetEnumerator();
            List<byte[]> res = new();
            while (peersEnumerator.MoveNext())
            {
                Array peer = (Array)peersEnumerator.Current;
                if (peer.Count != 1) throw new Exception(string.Format("unexpected stack item count (PeerInfo): expected {0}, has {1}", 1, peer.Count));
                IEnumerator<StackItem> peerEnumerator = peer.GetEnumerator();
                while (peerEnumerator.MoveNext())
                {
                    res.Add(peerEnumerator.Current.GetSpan().ToArray());
                }
            }
            return new(res.Select(p => NodeInfo.Parser.ParseFrom(p)).ToList().InfoToNodes());
        }

        public NetMap EpochSnapshot(ulong epoch)
        {
            InvokeResult result = TestInvoke(NetMapContractHash, EpochSnapshotMethod, epoch);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (EpochSnapshot)");
            if (result.ResultStack.Length != 1) throw new Exception(string.Format("unexpected stack item count ({0})", result.ResultStack.Length));
            Array peers = (Array)result.ResultStack[0];
            IEnumerator<StackItem> peersEnumerator = peers.GetEnumerator();
            List<byte[]> res = new();
            while (peersEnumerator.MoveNext())
            {
                Array peer = (Array)peersEnumerator.Current;
                if (peer.Count != 1) throw new Exception(string.Format("unexpected stack item count (PeerInfo): expected {0}, has {1}", 1, peer.Count));
                IEnumerator<StackItem> peerEnumerator = peer.GetEnumerator();
                while (peerEnumerator.MoveNext())
                {
                    res.Add(peerEnumerator.Current.GetSpan().ToArray());
                }
            }
            return new(res.Select(p => NodeInfo.Parser.ParseFrom(p)).ToList().InfoToNodes());
        }

        public bool ApprovePeer(byte[] peer)
        {
            return Invoke(out _, NetMapContractHash, AddPeerMethod, SideChainFee, peer);
        }

        public bool SetConfig(byte[] Id, byte[] key, byte[] value)
        {
            return Invoke(out _, NetMapContractHash, SetConfigMethod, SideChainFee, Id, key, value);
        }

        public bool SetInnerRing(ECPoint[] publicKeys)
        {
            var array = new ContractParameter(ContractParameterType.Array);
            var list = new List<ContractParameter>();
            foreach (var publicKey in publicKeys)
            {
                list.Add(new ContractParameter(ContractParameterType.PublicKey) { Value = publicKey });
            }
            array.Value = list;
            return Invoke(out _, NetMapContractHash, SetInnerRingMethod, SideChainFee, array);
        }
    }
}
