using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.Morph.Invoker;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.Wallets;

namespace Neo.FileStorage.Tests.Morph.Invoker
{
    [TestClass]
    public class UT_MorphClient : TestKit
    {
        private NeoSystem system;
        private MorphInvoker invoker;
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            system = TestBlockchain.TheNeoSystem;
            wallet = TestBlockchain.wallet;
            system.ActorSystem.ActorOf(Props.Create(() => new ProcessorFakeActor()));
            invoker = new MorphInvoker()
            {
                Wallet = wallet,
                NeoSystem = system,
                Blockchain = this.TestActor
            };
        }

        [TestMethod]
        public void InvokeLocalFunctionTest()
        {
            InvokeResult result = invoker.TestInvoke(NativeContract.GAS.Hash, "balanceOf", UInt160.Zero);
            Assert.AreEqual(result.State, VM.VMState.HALT);
            Assert.AreEqual(result.GasConsumed, 2028330);
            Assert.AreEqual(result.ResultStack[0].GetInteger(), 0);
        }

        [TestMethod]
        public void InvokeFunctionTest()
        {
            invoker.Invoke(out _, NativeContract.GAS.Hash, "balanceOf", 0, UInt160.Zero);
            var result = ExpectMsg<Transaction>();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TransferGasTest()
        {
            invoker.TransferGas(UInt160.Zero, 0);
            var result = ExpectMsg<Transaction>();
            Assert.IsNotNull(result);
        }
    }
}
