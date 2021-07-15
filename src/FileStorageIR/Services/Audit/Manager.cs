using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Neo.FileStorage.InnerRing.Services.Audit.Auditor;
using Neo.FileStorage.Utils;

namespace Neo.FileStorage.InnerRing.Services.Audit
{
    public class Manager : UntypedActor
    {
        public const int DefaultCapacity = 100;
        public class ResetMessage { }
        private readonly int taskQueueCapacity = DefaultCapacity;
        private readonly IContainerCommunicator communicator;
        private readonly ulong maxPDPIntervalMilliseconds;
        private readonly Queue<AuditTask> taskQueue;
        private readonly IActorRef workPool;
        private readonly Func<IActorRef> porPoolGenerator;
        private readonly Func<IActorRef> pdpPoolGenerator;

        public Manager(int capacity, IActorRef wp, Func<IActorRef> por_pool_generator, Func<IActorRef> pdp_pool_generator, IContainerCommunicator container_communicator, ulong max_pdp_interval)
        {
            taskQueueCapacity = capacity;
            taskQueue = new Queue<AuditTask>(taskQueueCapacity);
            communicator = container_communicator;
            maxPDPIntervalMilliseconds = max_pdp_interval;
            porPoolGenerator = por_pool_generator;
            pdpPoolGenerator = pdp_pool_generator;
            workPool = wp;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case AuditTask task:
                    NewTask(task);
                    break;
                case ResetMessage _:
                    Sender.Tell(Reset());
                    break;
            }
        }

        private void NewTask(AuditTask task)
        {
            if (taskQueue.Count < taskQueueCapacity)
            {
                taskQueue.Enqueue(task);
            }
            HandleTask();
        }

        private void HandleTask()
        {
            if (taskQueue.TryPeek(out AuditTask task))
            {
                task.Auditor = new Context
                {
                    ContainerCommunacator = communicator,
                    AuditTask = task,
                    MaxPDPInterval = maxPDPIntervalMilliseconds,
                    PorPool = porPoolGenerator(),
                    PdpPool = pdpPoolGenerator()
                };
                Task t = new(() =>
                {
                    task.Auditor.Execute();
                    HandleTask();
                });
                if ((bool)workPool.Ask(new WorkerPool.NewTask() { Process = "AuditManager", Task = t }).Result)
                {
                    taskQueue.Dequeue();
                }
            }
        }

        private int Reset()
        {
            var count = taskQueue.Count;
            taskQueue.Clear();
            return count;
        }

        public static Props Props(int capacity, IActorRef wp, Func<IActorRef> por_pool_generator, Func<IActorRef> pdp_pool_generator, IContainerCommunicator container_communicator, ulong max_pdp_interval)
        {
            return Akka.Actor.Props.Create(() => new Manager(capacity, wp, por_pool_generator, pdp_pool_generator, container_communicator, max_pdp_interval));
        }
    }
}
