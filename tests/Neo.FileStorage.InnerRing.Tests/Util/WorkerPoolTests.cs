using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.Utils;
using static Neo.FileStorage.Utils.WorkerPool;

namespace Neo.FileStorage.Tests.Util
{
    [TestClass]
    public class WorkerPoolTests : TestKit
    {
        private IActorRef workerpool;

        [TestInitialize]
        public void TestSetup()
        {
            workerpool = Sys.ActorOf(WorkerPool.Props("test", 2));
        }

        [TestMethod]
        public void NewTaskAndCompleteTaskTest()
        {
            Assert.IsTrue(workerpool.Ask<bool>(new NewTask() { Process = "aaa", Task = new Task(() => { Console.WriteLine("neofs"); }) }).Result);
        }

        [TestMethod]
        public void TestTooManyTask()
        {
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(workerpool.Ask<bool>(new NewTask() { Process = "aaa", Task = new Task(() => Thread.Sleep(10000)) }).Result);
            }
            Assert.IsFalse(workerpool.Ask<bool>(new NewTask() { Process = "aaa", Task = new Task(() => Console.WriteLine("neofs")) }).Result);
        }

        [TestMethod]
        public void TestTaskWait()
        {
            Task[] tasks = new Task[1];
            tasks[0] = new Task(() => { });
            Assert.IsTrue(workerpool.Ask<bool>(new WorkerPool.NewTask { Process = "TestTaskWait", Task = tasks[0] }).Result);
            Thread.Sleep(2000);
            Task.WaitAll(tasks);
            Console.WriteLine("success!");
        }
    }
}
