using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.API.Container;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.Session;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Wallets;

namespace Neo.FileStorage.Tests.Morph.Invoker
{
    [TestClass]
    public class UT_MorphContractInvoker : TestKit
    {
        private FileStorage.Morph.Invoker.MorphInvoker invoker;
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            NeoSystem system = TestBlockchain.TheNeoSystem;
            wallet = TestBlockchain.wallet;
            system.ActorSystem.ActorOf(Props.Create(() => new ProcessorFakeActor()));
            invoker = new FileStorage.Morph.Invoker.MorphInvoker()
            {
                Wallet = wallet,
                NeoSystem = system,
                Blockchain = this.TestActor
            };
        }

        [TestMethod]
        public void InvokeBalanceOfTest()
        {
            long result = invoker.BalanceOf(UInt160.Zero.ToArray());
            Assert.AreEqual(result, 0);
        }

        [TestMethod]
        public void InvokeDecimalsTest()
        {
            long result = invoker.BalanceDecimals();
            Assert.AreEqual(result, 12);
        }

        [TestMethod]
        public void InvokeAddPeerTest()
        {
            var key = wallet.GetAccounts().ToArray()[0].GetKey().PublicKey;
            NodeInfo nodeInfo = new NodeInfo();
            nodeInfo.Address = Neo.FileStorage.API.Cryptography.KeyExtension.PublicKeyToAddress(key.ToArray());
            nodeInfo.PublicKey = ByteString.CopyFrom(key.ToArray());
            bool result = invoker.AddPeer(nodeInfo);
            Assert.AreEqual(result, true);
        }

        [TestMethod]
        public void InvokeConfigTest()
        {
            ulong result = invoker.ContainerFee();
            Assert.AreEqual(0ul, result);
        }

        [TestMethod]
        public void InvokeEpochTest()
        {
            ulong result = invoker.Epoch();
            Assert.AreEqual(result, 1uL);
        }

        [TestMethod]
        public void InvokeNewEpochTest()
        {
            bool result = invoker.NewEpoch(2);
            var tx = ExpectMsg<Transaction>();
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeUpdateStateTest()
        {
            var key = wallet.GetAccounts().ToArray()[0].GetKey().PublicKey;
            bool result = invoker.UpdatePeerState(NodeInfo.Types.State.Online, key.EncodePoint(true));
            var tx = ExpectMsg<Transaction>();
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeSnapshotTest()
        {
            var result = invoker.Snapshot(0);
            Assert.AreEqual(result.Nodes.Count, 1);
        }

        [TestMethod]
        public void InvokeNetMapTest()
        {
            var result = invoker.NetMap();
            Assert.AreEqual(result.Length, 1);
        }

        [TestMethod]
        public void InvokePutTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair kp = accounts.ToArray()[0].GetKey();
            ECDsa key = kp.PrivateKey.LoadPrivateKey();
            OwnerID owner = key.ToOwnerID();
            Container container = new Container()
            {
                Version = new API.Refs.Version(),
                BasicAcl = 0,
                Nonce = ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = owner,
                PlacementPolicy = new PlacementPolicy()
            };
            Signature sig = key.SignMessagePart(container);
            SessionToken token = new()
            {
                Body = new()
                {
                    OwnerId = owner,
                    Id = ByteString.CopyFrom(Guid.NewGuid().ToByteArray()),
                    SessionKey = ByteString.CopyFrom(Guid.NewGuid().ToByteArray()),
                    Lifetime = new()
                }
            };
            token.Signature = key.SignMessagePart(token.Body);
            bool result = invoker.PutContainer(container, sig, token);
            var tx = ExpectMsg<Transaction>();
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeDeleteTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair kp = accounts.ToArray()[0].GetKey();
            ECDsa key = kp.PrivateKey.LoadPrivateKey();
            OwnerID ownerId = key.ToOwnerID();
            Container container = new Container()
            {
                Version = new API.Refs.Version(),
                BasicAcl = 0,
                Nonce = ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new PlacementPolicy()
            };
            byte[] sig = key.SignRFC6979(container.ToByteArray());
            SessionToken token = new()
            {
                Body = new()
                {
                    OwnerId = ownerId,
                    Id = ByteString.CopyFrom(Guid.NewGuid().ToByteArray()),
                    SessionKey = ByteString.CopyFrom(Guid.NewGuid().ToByteArray()),
                    Lifetime = new()
                }
            };
            token.Signature = key.SignMessagePart(token.Body);
            bool result = invoker.DeleteContainer(container.CalCulateAndGetId, sig, token);
            var tx = ExpectMsg<Transaction>();
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeSetEACLTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair kp = accounts.ToArray()[0].GetKey();
            ECDsa key = kp.PrivateKey.LoadPrivateKey();
            OwnerID ownerId = key.ToOwnerID();
            Container container = new Container()
            {
                Version = new API.Refs.Version(),
                BasicAcl = 0,
                Nonce = ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new PlacementPolicy()
            };
            API.Acl.EACLTable eACLTable = new API.Acl.EACLTable()
            {
                ContainerId = container.CalCulateAndGetId,
                Version = new API.Refs.Version(),
            };
            eACLTable.Records.Add(new API.Acl.EACLRecord());
            Signature sig = key.SignMessagePart(eACLTable);
            SessionToken token = new()
            {
                Body = new()
                {
                    OwnerId = ownerId,
                    Id = ByteString.CopyFrom(Guid.NewGuid().ToByteArray()),
                    SessionKey = ByteString.CopyFrom(Guid.NewGuid().ToByteArray()),
                    Lifetime = new()
                }
            };
            token.Signature = key.SignMessagePart(token.Body);
            bool result = invoker.SetEACL(eACLTable, sig, token);
            var tx = ExpectMsg<Transaction>();
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod]
        public void InvokeGetEACLTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            Neo.FileStorage.API.Refs.OwnerID ownerId = Neo.FileStorage.API.Cryptography.KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new Neo.FileStorage.API.Refs.Version(),
                BasicAcl = 0,
                Nonce = ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new PlacementPolicy()
            };
            API.Acl.EACLTable eACLTable = new API.Acl.EACLTable()
            {
                ContainerId = container.CalCulateAndGetId,
                Version = new API.Refs.Version(),
            };
            eACLTable.Records.Add(new API.Acl.EACLRecord());
            byte[] sig = key.PrivateKey.LoadPrivateKey().SignRFC6979(eACLTable.ToByteArray());
            EAclWithSignature result = invoker.GetEACL(container.CalCulateAndGetId);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Table.ToByteArray().ToHexString(), eACLTable.ToByteArray().ToHexString());
        }

        [TestMethod]
        public void InvokeGetContainerTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            API.Refs.OwnerID ownerId = API.Cryptography.KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new API.Refs.Version(),
                BasicAcl = 0,
                Nonce = ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new PlacementPolicy()
            };
            ContainerWithSignature result = invoker.GetContainer(container.CalCulateAndGetId);
            Assert.AreEqual(result.Container.ToByteArray().ToHexString(), container.ToByteArray().ToHexString());
        }

        [TestMethod]
        public void InvokeGetContainerListTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            API.Refs.OwnerID ownerId = KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new API.Refs.Version(),
                BasicAcl = 0,
                Nonce = ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new PlacementPolicy()
            };
            List<ContainerID> result = invoker.ListContainers(ownerId);
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result.ElementAt(0).ToByteArray().ToHexString(), container.CalCulateAndGetId.ToByteArray().ToHexString());
        }
    }
}
