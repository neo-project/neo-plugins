using System;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.Morph.Event;

namespace Neo.FileStorage.Tests.Morph.Event
{
    [TestClass]
    public class UT_BlockTimer : TestKit
    {
        private NeoSystem system;

        [TestInitialize]
        public void TestSetup()
        {
            system = TestBlockchain.TheNeoSystem;
        }

        [TestMethod]
        public void OnTickTest()
        {
            uint dur = 2;
            var blockTimer = new BlockTimer(() => { return dur; }, () => { this.TestActor.Tell(new Object()); });
            blockTimer.Reset();
            blockTimer.Tick();
            ExpectNoMsg();
            blockTimer.Tick();
            ExpectMsg<Object>();
        }

        [TestMethod]
        public void OnDelta()
        {
            var blockTimer = new BlockTimer(() => { return 1; }, () => { });
            blockTimer.Delta(
                2,
                1,
                () => { this.TestActor.Tell(new Object()); }
                );
            blockTimer.Reset();
            blockTimer.Tick();
            ExpectNoMsg();
            blockTimer.Tick();
            ExpectMsg<Object>();
        }
    }
}
