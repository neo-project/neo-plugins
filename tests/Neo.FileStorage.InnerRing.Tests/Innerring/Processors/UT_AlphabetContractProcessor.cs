using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.InnerRing.Invoker;
using Neo.FileStorage.InnerRing.Processors;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Tests;
using Neo.Wallets;
using static Neo.FileStorage.InnerRing.Timer.TimerTickEvent;

namespace Neo.FileStorage.InnerRing.Tests.InnerRing.Processors
{
    [TestClass]
    public class UT_AlphabetContractProcessor : TestKit
    {
        private NeoSystem system;
        private AlphabetContractProcessor processor;
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
            processor = new AlphabetContractProcessor()
            {

                MorphCli = morphInvoker,
                State = state,
                WorkPool = actor
            };
        }

        [TestMethod]
        public void HandleHandleGasEmissionTest()
        {
            processor.HandleGasEmission(new NewAlphabetEmitTickEvent());
            var nt = ExpectMsg<ProcessorFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod]
        public void ProcessEmitTest()
        {
            state.isAlphabet = true;
            state.alphabetIndex = 1;
            processor.ProcessEmit();
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.IsNotNull(tx);
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
