using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.InnerRing;
using Neo.FileStorage.InnerRing.Invoker;
using Neo.FileStorage.InnerRing.Processors;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Tests;
using Neo.Wallets;
using static Neo.FileStorage.InnerRing.Events.MorphEvent;
using static Neo.FileStorage.InnerRing.Processors.SettlementProcessor;

namespace Neo.FileStorage.InnerRing.Tests.InnerRing.Processors
{
    [TestClass]
    public class UT_SettlementProcessor : TestKit
    {
        private NeoSystem system;
        private SettlementProcessor processor;
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
            var clientCache = new RpcClientCache() { wallet = wallet };
            var auditCalcDeps = new AuditSettlementDeps()
            {
                Invoker = morphInvoker,
                clientCache = clientCache,
            };
            var basicSettlementDeps = new BasicIncomeSettlementDeps()
            {
                Invoker = morphInvoker,
            };
            var auditSettlementCalc = new Calculator(auditCalcDeps);
            processor = new SettlementProcessor()
            {
                basicIncome = basicSettlementDeps,
                auditProc = auditSettlementCalc,
                State = state,
                WorkPool = actor
            };
        }

        [TestMethod]
        public void HandleTest()
        {
            state.isActive = true;
            processor.Handle(0);
            ExpectNoMsg();
            processor.Handle(1);
            ExpectNoMsg();
        }

        [TestMethod]
        public void HandleAuditEventTest()
        {
            processor.HandleAuditEvent(new AuditStartEvent() { epoch = 1 });
            var nt = ExpectMsg<ProcessorFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod]
        public void HandleIncomeCollectionEventTest()
        {
            state.isAlphabet = true;
            processor.HandleIncomeCollectionEvent(new BasicIncomeCollectEvent() { epoch = 1 });
            var nt = ExpectMsg<ProcessorFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod]
        public void HandleIncomeDistributionEventTest()
        {
            state.isAlphabet = true;
            processor.HandleIncomeDistributionEvent(new BasicIncomeDistributeEvent() { epoch = 1 });
            ExpectNoMsg();
        }

        [TestMethod]
        public void ListenerHandlersTest()
        {
            var handlerInfos = processor.ListenerHandlers();
            Assert.AreEqual(0, handlerInfos.Length);
        }

        [TestMethod]
        public void ListenerParsersTest()
        {
            var parserInfos = processor.ListenerParsers();
            Assert.AreEqual(0, parserInfos.Length);
        }
    }
}
