using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Actor;
using Neo.Cryptography.ECC;
using Neo.FileStorage.Morph.Invoker;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;

namespace Neo.FileStorage.InnerRing.Invoker
{
    public partial class MainInvoker
    {
        public Wallet Wallet { get; init; }
        public NeoSystem NeoSystem { get; init; }
        public IActorRef Blockchain { get; init; }
        public long MainChainFee { get; init; }
        public UInt160 FsContractHash { get; init; }

        public bool Invoke(out UInt256 txId, UInt160 contractHash, string method, long fee, params object[] args)
        {
            txId = null;
            InvokeResult result = TestInvoke(contractHash, method, args);
            if (result.State != VMState.HALT) return false;
            SnapshotCache snapshot = NeoSystem.GetSnapshot();
            uint height = NativeContract.Ledger.CurrentIndex(snapshot);
            Random rand = new Random();
            Transaction tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)rand.Next(),
                Script = result.Script,
                ValidUntilBlock = height + NeoSystem.Settings.MaxValidUntilBlockIncrement,
                Signers = new Signer[] { new Signer() { Account = Wallet.GetAccounts().ToArray()[0].ScriptHash, Scopes = WitnessScope.Global } },
                Attributes = System.Array.Empty<TransactionAttribute>(),
            };
            tx.SystemFee = result.GasConsumed + fee;
            tx.NetworkFee = Wallet.CalculateNetworkFee(snapshot, tx);
            var data = new ContractParametersContext(snapshot, tx, NeoSystem.Settings.Network);
            bool sigresult = Wallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            txId = tx.Hash;
            Blockchain.Tell(tx);
            var balance = Wallet.GetBalance(snapshot, NativeContract.GAS.Hash, Wallet.GetAccounts().ToArray()[0].ScriptHash);
            Utility.Log("client", LogLevel.Debug, string.Format("neo client invoke,method:{0},tx_hash:{1},current height：{2},SystemFee:{3},NetWorkFee:{4},balance:{5},isInsufficient:{6}", method, tx.Hash.ToString(), height, tx.SystemFee, tx.NetworkFee, balance.Value * 100000000, (tx.SystemFee + tx.NetworkFee) > ((long)balance.Value * 100000000)));
            return true;
        }

        public InvokeResult TestInvoke(UInt160 contractHash, string method, params object[] args)
        {
            byte[] script = contractHash.MakeScript(method, args);
            IEnumerable<WalletAccount> accounts = Wallet.GetAccounts();
            FakeSigners signers = new FakeSigners(accounts.ToArray()[0].ScriptHash);
            return GetInvokeResult(script, signers);
        }

        private InvokeResult GetInvokeResult(byte[] script, FakeSigners signers = null, bool testMode = true)
        {
            SnapshotCache snapshot = NeoSystem.GetSnapshot();
            ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, container: signers, null, NeoSystem.Settings, 0, 20000000000);
            return new InvokeResult() { State = engine.State, GasConsumed = (long)engine.GasConsumed, Script = script, ResultStack = engine.ResultStack.ToArray<StackItem>() };
        }
        public ECPoint[] Committee()
        {
            var result = TestInvoke(NativeContract.NEO.Hash, "getCommittee");
            return ((VM.Types.Array)result.ResultStack[0]).Select(p => p.GetSpan().AsSerializable<ECPoint>()).ToArray();
        }

        public ECPoint[] NeoFSAlphabetList()
        {
            var height = TestInvoke(NativeContract.Ledger.Hash, "currentIndex").ResultStack[0].GetInteger();
            var result = TestInvoke(NativeContract.RoleManagement.Hash, "getDesignatedByRole", Role.NeoFSAlphabetNode, height);
            return ((VM.Types.Array)result.ResultStack[0]).Select(p => p.GetSpan().AsSerializable<ECPoint>()).ToArray();
        }
    }
}
