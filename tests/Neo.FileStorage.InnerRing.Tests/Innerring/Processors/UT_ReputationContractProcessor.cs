using System.Linq;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Reputation;
using Neo.FileStorage.InnerRing.Invoker;
using Neo.FileStorage.InnerRing.Processors;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Tests;
using Neo.IO;
using Neo.Wallets;
using static Neo.FileStorage.Morph.Event.MorphEvent;

namespace Neo.FileStorage.InnerRing.Tests.InnerRing.Processors
{
    [TestClass]
    public class UT_ReputationContractProcessor : TestKit
    {
        private NeoSystem system;
        private ReputationContractProcessor processor;
        private MorphInvoker morphInvoker;
        private MainInvoker mainInvoker;
        private Wallet wallet;
        private TestState state;
        private IActorRef actor;

        [TestInitialize]
        public void TestSetup()
        {
            system = TestBlockchain.TheNeoSystem;
            wallet = TestBlockchain.wallet;
            actor = this.ActorOf(Props.Create(() => new ProcessorFakeActor()));
            mainInvoker = new MainInvoker
            {
                Wallet = wallet,
                NeoSystem = system,
                Blockchain = actor,
            };
            morphInvoker = new MorphInvoker()
            {
                Wallet = wallet,
                NeoSystem = system,
                Blockchain = actor,
            };
            state = new TestState() { alphabetIndex = 1 };
            processor = new ReputationContractProcessor()
            {
                MorphCli = morphInvoker,
                State = state,
                WorkPool = actor
            };
        }

        [TestMethod]
        public void HandlePutReputationTest()
        {
            byte[] publicKey = wallet.GetAccounts().Select(p => p.GetKey().PublicKey).ToArray()[0].ToArray();
            processor.HandlePutReputation(new ReputationPutEvent()
            {
                Epoch = 1,
                PeerID = publicKey,
                Trust = null
            });
            var nt = ExpectMsg<ProcessorFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod]
        public void ProcessPutTest()
        {
            byte[] privateKey = wallet.GetAccounts().Select(p => p.GetKey().PrivateKey).ToArray()[0].ToArray();
            byte[] publicKey = wallet.GetAccounts().Select(p => p.GetKey().PublicKey).ToArray()[0].ToArray();
            GlobalTrust gt = new()
            {
                Body = new()
                {
                    Manager = new()
                    {
                        PublicKey = ByteString.CopyFrom(publicKey),
                    },
                    Trust = new()
                    {
                        Peer = new()
                        {
                            PublicKey = ByteString.CopyFrom(publicKey),
                        },
                        Value = 1.1,
                    }
                }
            };
            gt.Signature = KeyExtension.LoadPrivateKey(privateKey).SignMessagePart(gt.Body);
            state.isAlphabet = true;
            state.SetEpochCounter(2);
            processor.ProcessPut(new ReputationPutEvent()
            {
                Epoch = 0,
                PeerID = publicKey,
                Trust = gt
            });
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.IsNotNull(tx);
            state.isAlphabet = false;
            processor.ProcessPut(new ReputationPutEvent());
            ExpectNoMsg();
        }

        [TestMethod]
        public void ListenerHandlersTest()
        {
            var handlerInfos = processor.ListenerHandlers();
            Assert.AreEqual(1, handlerInfos.Length);
        }

        [TestMethod]
        public void ListenerParsersTest()
        {
            var parserInfos = processor.ListenerParsers();
            Assert.AreEqual(1, parserInfos.Length);
        }
    }
}
