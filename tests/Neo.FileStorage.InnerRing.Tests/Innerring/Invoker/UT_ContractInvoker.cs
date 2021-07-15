using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.InnerRing.Invoker;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Tests;
using Neo.IO;
using Neo.Wallets;
using Container = Neo.FileStorage.API.Container.Container;

namespace Neo.FileStorage.InnerRing.Tests.InnerRing.Invoker
{
    [TestClass]
    public class UT_ContractInvoker : TestKit
    {
        private MorphInvoker morphInvoker;
        private MainInvoker mainInvoker;
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            NeoSystem system = TestBlockchain.TheNeoSystem;
            wallet = TestBlockchain.wallet;
            mainInvoker = new MainInvoker
            {
                Wallet = wallet,
                NeoSystem = system,
                Blockchain = this.ActorOf(Props.Create(() => new ProcessorFakeActor()))
            };
            morphInvoker = new MorphInvoker()
            {
                Wallet = wallet,
                NeoSystem = system,
                Blockchain = this.ActorOf(Props.Create(() => new ProcessorFakeActor()))
            };
        }

        [TestMethod]
        public void InvokeMintTest()
        {
            bool result = morphInvoker.Mint(FileStorage.InnerRing.Settings.Default.NetmapContractHash.ToArray(), 0, new byte[] { 0x01 });
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeBurnTest()
        {
            bool result = morphInvoker.Burn(FileStorage.InnerRing.Settings.Default.NetmapContractHash.ToArray(), 0, new byte[] { 0x01 });
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeLockAssetTest()
        {
            bool result = morphInvoker.LockAsset(new byte[] { 0x01 }, UInt160.Zero, UInt160.Zero, 0, 100);
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeBalancePrecisionTest()
        {
            uint result = morphInvoker.BalancePrecision();
            Assert.AreEqual(result, (uint)12);
        }

        [TestMethod]
        public void InvokeRegisterContainerTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            OwnerID ownerId = API.Cryptography.KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new API.Refs.Version(),
                BasicAcl = 0,
                Nonce = ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new PlacementPolicy()
            };
            byte[] sig = Cryptography.Crypto.Sign(container.ToByteArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            bool result = morphInvoker.RegisterContainer(key.PublicKey.ToArray(), container.ToByteArray(), sig, null);
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeRemoveContainerTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            var containerId = "fc780e98b7970002a80fbbeb60f9ed6cf44d5696588ea32e4338ceaeda4adddc".HexToBytes();
            var sig = Cryptography.Crypto.Sign(containerId, key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            var result = morphInvoker.RemoveContainer(containerId, sig, null);
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeGetEpochTest()
        {
            ulong result = morphInvoker.Epoch();
            Assert.AreEqual(result, (ulong)1);
        }

        [TestMethod]
        public void InvokeSetNewEpochTest()
        {
            bool result = morphInvoker.NewEpoch(100);
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeApproveAndUpdatePeerTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            var nodeInfo = new NodeInfo()
            {
                PublicKey = ByteString.CopyFrom(key.PublicKey.ToArray()),
                Address = API.Cryptography.KeyExtension.PublicKeyToAddress(key.PublicKey.ToArray()),
                State = NodeInfo.Types.State.Online
            };
            bool result = morphInvoker.ApprovePeer(nodeInfo.ToByteArray());
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
            result = morphInvoker.UpdatePeerState(NodeInfo.Types.State.Offline, key.PublicKey.EncodePoint(true));
            tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeSetConfigTest()
        {
            bool result = morphInvoker.SetConfig(new byte[] { 0x01 }, Utility.StrictUTF8.GetBytes("ContainerFee"), BitConverter.GetBytes(0));
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeSetInnerRingTest()
        {
            bool result = morphInvoker.SetInnerRing(wallet.GetAccounts().Select(p => p.GetKey().PublicKey).ToArray());
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeNetmapSnapshotTest()
        {
            NodeInfo[] result = morphInvoker.NetMap();
            Assert.AreEqual(result.Length, 1);
        }

        [TestMethod]
        public void InvokeAlphabetEmitTest()
        {
            bool result = morphInvoker.AlphabetEmit(1);
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeAlphabetVoteTest()
        {
            bool result = morphInvoker.AlphabetVote(0, 1, wallet.GetAccounts().Select(p => p.GetKey().PublicKey).ToArray());
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeCashOutChequeTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            bool result = mainInvoker.CashOutCheque(new byte[] { 0x01 }, 1, accounts.ToArray()[0].ScriptHash, accounts.ToArray()[0].ScriptHash);
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeInnerRingIndexTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            morphInvoker.InnerRingIndex(accounts.ToArray()[0].GetKey().PublicKey, out int index, out int length);
            Assert.AreEqual(index, 1);
            Assert.AreEqual(length, 7);
        }

        [TestMethod]
        public void InvokeAlphabetIndexTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            int index = morphInvoker.AlphabetIndex(accounts.ToArray()[0].GetKey().PublicKey);
            Assert.AreEqual(index, 1);
        }

        [TestMethod]
        public void InvokeAlphabetUpdateTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            bool result = mainInvoker.AlphabetUpdate(new byte[1] { 0x01 }, accounts.Select(p => p.GetKey().PublicKey).ToArray());
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        /*        [TestMethod]
                public void InvokePutAuditResultTest()
                {
                    morphInvoker.InvokePutAuditResult();
                    { "auditEpoch": "15", "containerID": { "value": "ffiBhFJACt+F1fABuKsfJZZ3PjA04SFp9P7tL2tmncA=" }, "publicKey": "ArNiK/QBe9/jF8WK7V9MdT8ga324lgRvp9d0u8S/f43C", "complete": true, "requests": 1, "passSG": [ { "value": "5Gv2Wk0MHyAOZdhhA5PBGGJ6aUryuA8MgXFstAJl4iA=" } ], "hit": 1 }
                    IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
                    bool result = morphInvoker.AlphabetUpdate(new byte[1] { 0x01 }, accounts.Select(p => p.GetKey().PublicKey).ToArray());
                    var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
                    Assert.AreEqual(result, true);
                    Assert.IsNotNull(tx);
                }*/
    }
}
