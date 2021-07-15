using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.StorageGroup;
using Neo.FileStorage.InnerRing.Services.Audit;
using Neo.FileStorage.InnerRing.Services.Audit.Auditor;
using Neo.FileStorage.Utils;
using FSObject = Neo.FileStorage.API.Object.Object;
using FSRange = Neo.FileStorage.API.Object.Range;

namespace Neo.FileStorage.Tests.Services.Audit
{
    [TestClass]
    public class UT_Manager : TestKit
    {
        public const int ManagerCapacity = 3;
        private IActorRef manager;

        public class WorkerPoolWrapper : UntypedActor
        {
            private readonly IActorRef r;
            private readonly IActorRef wp;

            public WorkerPoolWrapper(IActorRef r, IActorRef wp)
            {
                this.wp = wp;
                this.r = r;
            }

            protected override void OnReceive(object message)
            {
                if (message is WorkerPool.NewTask t)
                {
                    bool result = wp.Ask<bool>(new WorkerPool.NewTask()
                    {
                        Process = t.Process,
                        Task = new(() => Thread.Sleep(60000))
                    }).Result;
                    Sender.Tell(result);
                    r.Tell(result);
                }
            }
        }

        public class Reporter : IReporter
        {
            public void WriteReport(Report r)
            {
                throw new NotImplementedException();
            }
        }

        public class TestContainerCommunacator : IContainerCommunicator
        {
            public StorageGroup GetStorageGroup(AuditTask task, ObjectID oid)
            {
                throw new NotImplementedException();
            }

            public FSObject GetHeader(AuditTask task, Node node, ObjectID oid, bool relay)
            {
                throw new NotImplementedException();
            }

            public byte[] GetRangeHash(AuditTask task, Node node, ObjectID oid, FSRange range)
            {
                throw new NotImplementedException();
            }
        }

        [TestInitialize]
        public void TestSetup()
        {
            ulong interval = 5000;
            CancellationTokenSource source = new();
            IActorRef wp = Sys.ActorOf(WorkerPool.Props("Audit", ManagerCapacity));
            IActorRef fwp = Sys.ActorOf(Props.Create(() => new WorkerPoolWrapper(TestActor, wp)));
            manager = Sys.ActorOf(Manager.Props(ManagerCapacity, fwp, () =>
            {
                return Sys.ActorOf(WorkerPool.Props("AuditPOR", ManagerCapacity));
            }, () =>
            {
                return Sys.ActorOf(WorkerPool.Props("AuditPDP", ManagerCapacity));
            }, new TestContainerCommunacator(), interval));
        }

        [TestMethod]
        public void TestOneTask()
        {
            manager.Tell(new AuditTask());
            Assert.IsTrue(ExpectMsg<bool>());
        }

        [TestMethod]
        public void TestRedundantTaskAndReset()
        {
            int redundant = 3;
            for (int i = 0; i < ManagerCapacity; i++)
            {
                manager.Tell(new AuditTask());
                Assert.IsTrue(ExpectMsg<bool>());
            }
            for (int i = 0; i < redundant; i++)
            {
                manager.Tell(new AuditTask());
                Assert.IsFalse(ExpectMsg<bool>());
            }
            Assert.AreEqual(redundant, manager.Ask<int>(new Manager.ResetMessage()).Result);
        }
    }
}
