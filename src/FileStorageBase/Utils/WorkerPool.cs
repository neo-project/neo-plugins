using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;

namespace Neo.FileStorage.Utils
{
    /// <summary>
    /// WorkerPool is a thread pool that uses Akka's message mechanism internally.
    /// Multiple instances can be created, and the task scheduling is independent.
    /// Through its internal timer, it will periodically select a certain count of
    /// tasks from the task list for execution.
    /// [free=capacity-running]
    /// </summary>
    public class WorkerPool : UntypedActor
    {
        private readonly string name;
        private readonly int capacity;
        private int running;

        public class NewTask { public string Process; public Task Task; };
        public class CompleteTask { };

        public WorkerPool(string name, int capacity)
        {
            this.name = name;
            this.capacity = capacity;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case NewTask newTask:
                    OnNewTask(newTask);
                    break;
                case CompleteTask _:
                    OnCompleteTask();
                    break;
                default:
                    break;
            }
        }

        private void OnNewTask(NewTask newTask)
        {
            IActorRef actor = Self;
            if (capacity <= running)
            {
                Utility.Log(newTask.Process, LogLevel.Warning, string.Format("worker pool drained,capacity:{0}", capacity.ToString()));
                Sender.Tell(false);
            }
            else
            {
                newTask.Task.ContinueWith(t => { actor.Tell(new CompleteTask()); });
                newTask.Task.Start();
                running++;
                Sender.Tell(true);
            }
        }

        private void OnCompleteTask()
        {
            running--;
        }

        public static Props Props(string name, int capacity)
        {
            return Akka.Actor.Props.Create(() => new WorkerPool(name, capacity));
        }
    }
}
