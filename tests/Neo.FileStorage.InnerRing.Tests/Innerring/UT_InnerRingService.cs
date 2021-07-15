using System;
using System.Linq;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.InnerRing;
using Neo.FileStorage.InnerRing.Invoker;
using Neo.FileStorage.Morph.Invoker;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets.NEP6;
using static Neo.FileStorage.InnerRing.InnerRingService;

namespace Neo.FileStorage.Tests.InnerRing
{
    [TestClass]
    public class UT_InnerRingService : TestKit
    {
        private NeoSystem system;
        private NEP6Wallet wallet;
        private IActorRef innerring;
        private MorphInvoker morphInvoker;
        private MainInvoker mainInvoker;

        [TestInitialize]
        public void TestSetup()
        {
            system = TestBlockchain.TheNeoSystem;
            wallet = TestBlockchain.wallet;
            mainInvoker = new MainInvoker
            {
                Wallet = wallet,
                NeoSystem = system,
                Blockchain = TestActor,
            };
            morphInvoker = new MorphInvoker()
            {
                Wallet = wallet,
                NeoSystem = system,
                Blockchain = TestActor
            };
            innerring = system.ActorSystem.ActorOf(Props(system, system, wallet, wallet, mainInvoker, morphInvoker));
        }

        [TestMethod]
        public void InitConfigAndContractEventTest()
        {
            innerring.Tell(new Start());
            var tx = ExpectMsg<Transaction>();
            Assert.IsNotNull(tx);
            using var snapshot = system.GetSnapshot();
            //create notify
            tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 0,
                Nonce = 0,
                Script = new byte[] { 0x01 },
                Signers = new Signer[] { new Signer() { Account = wallet.GetAccounts().ToArray()[0].ScriptHash } },
                SystemFee = 0,
                ValidUntilBlock = 0,
                Version = 0,
            };
            var data = new ContractParametersContext(snapshot, tx, system.Settings.Network);
            wallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            NotifyEventArgs notify = new(tx, UInt160.Zero, "test", new VM.Types.Array() { new VM.Types.Boolean(true) });
            innerring.Tell(new ContractEvent() { notify = notify });
            ExpectMsg<Transaction>();
            innerring.Tell(new Stop());
        }
    }
}
