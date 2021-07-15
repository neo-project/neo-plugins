using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.InnerRing.Invoker;
using Neo.FileStorage.InnerRing.Processors;
using Neo.FileStorage.InnerRing.Services.Audit;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Tests;
using Neo.Wallets;
using static Neo.FileStorage.Morph.Event.MorphEvent;

namespace Neo.FileStorage.InnerRing.Tests.InnerRing.Processors
{
    [TestClass]
    public class UT_AuditContractProcessor : TestKit
    {
        private NeoSystem system;
        private AuditContractProcessor processor;
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
            state = new TestState() { alphabetIndex = 1, innerRingIndex = 0, innerRingSize = 1 };
            var auditTaskManager = system.ActorSystem.ActorOf(FakeAuditTaskManager.Props(TestActor));
            var clientCache = new RpcClientCache() { wallet = wallet };
            processor = new AuditContractProcessor()
            {
                MorphCli = morphInvoker,
                ClientCache = clientCache,
                TaskManager = auditTaskManager,
                State = state,
                WorkPool = actor
            };
        }

        [TestMethod]
        public void HandleNewAuditRoundTest()
        {
            processor.HandleNewAuditRound(new StartEvent() { epoch = 1 });
            var nt = ExpectMsg<ProcessorFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod]
        public void ProcessStartAuditTest()
        {
            processor.ProcessStartAudit(1);
            var nt = ExpectMsg<AuditTask>();
            Assert.IsNotNull(nt);
        }
    }

    public class FakeAuditTaskManager : UntypedActor
    {
        private IActorRef actor;

        public FakeAuditTaskManager(IActorRef actor)
        {
            this.actor = actor;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case AuditTask task:
                    actor.Tell(task);
                    break;
                case Manager.ResetMessage _:
                    Sender.Tell(0);
                    break;
            }
        }

        public static Props Props(IActorRef actor)
        {
            return Akka.Actor.Props.Create(() => new FakeAuditTaskManager(actor));
        }
    }
}
