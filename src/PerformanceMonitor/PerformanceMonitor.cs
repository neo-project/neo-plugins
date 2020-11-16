using Akka.Actor;
using Neo.ConsoleService;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins
{
    public partial class PerformanceMonitor : Plugin, IPersistencePlugin, IP2PPlugin
    {
        private delegate void CommitHandler(StoreView snapshot);
        private delegate void PersistHandler(StoreView snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList);
        private delegate void P2PMessageHandler(Message message);
        private delegate void ConsensusMessageHandler(ConsensusPayload payload);

        public override string Name => "PerformanceMonitor";
        public override string Description => "Provides performance metrics commands for the node";

        public PerformanceMonitor()
        {
            RpcServerPlugin.RegisterMethods(this);
        }

        protected override void Configure()
        {
            Settings.Load(GetConfiguration());
        }

        private event CommitHandler OnCommitEvent;
        public void OnCommit(StoreView snapshot)
        {
            OnCommitEvent?.Invoke(snapshot);
        }

        private event PersistHandler OnPersistEvent;
        public void OnPersist(StoreView snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            OnPersistEvent?.Invoke(snapshot, applicationExecutedList);
        }

        private event P2PMessageHandler OnP2PMessageEvent;
        public bool OnP2PMessage(Message message)
        {
            OnP2PMessageEvent?.Invoke(message);
            return true;
        }

        private event ConsensusMessageHandler OnConsensusMessageEvent;
        public bool OnConsensusMessage(ConsensusPayload payload)
        {
            OnConsensusMessageEvent?.Invoke(payload);
            return true;
        }

        /// <summary>
        /// Process "check state" command
        /// Prints allocated memory, CPU usage and active threads every second
        /// </summary>
        [ConsoleCommand("check state", Category = "Node Commands", Description = "Show allocated memory, CPU usage and active threads every second.")]
        private void OnCheckStateCommand()
        {
            Console.Clear();
            var cancel = new CancellationTokenSource();

            Task task = Task.Run(async () =>
            {
                var monitor = new CpuUsageMonitor();

                while (!cancel.Token.IsCancellationRequested)
                {
                    try
                    {
                        var total = monitor.GetCpuTotalProcessorTime(true);
                        if (!cancel.Token.IsCancellationRequested)
                        {
                            Console.WriteLine($"Active threads: {monitor.ThreadCount,3}\tTotal CPU usage: {total,8:0.00 %}");
                            PrintAllocatedMemory();
                        }

                        await Task.Delay(1000, cancel.Token);
                    }
                    catch
                    {
                        // if any unexpected exception is thrown, stop the loop and finish the task
                        cancel.Cancel();
                    }
                }
            });
            Console.ReadLine();
            cancel.Cancel();
        }

        /// <summary>
        /// Gets node state information
        /// </summary>
        /// <returns>
        /// The total allocated memory, total CPU usage and the CPU usage of each active thread in the last second
        /// </returns>
        [RpcMethod]
        public JObject GetState(JArray _params)
        {
            if (_params.Count != 0)
            {
                throw new RpcException(-32602, "Invalid params");
            }
            var monitor = new CpuUsageMonitor();

            // wait a second to get the cpu usage info
            Thread.Sleep(1000);
            var cpuUsage = monitor.CheckAllThreads();

            var result = new JObject();
            result["totalusage"] = cpuUsage.TotalUsage;

            var threads = new JArray();
            foreach (var threadTime in cpuUsage.ThreadsUsage.OrderByDescending(usage => usage.Value))
            {
                var thread = new JObject();
                thread["id"] = threadTime.Key;
                thread["usage"] = threadTime.Value;
                threads.Add(thread);
            }
            result["threads"] = threads;
            result["memory"] = GetAllocatedMemory();

            return result;
        }

        /// <summary>
        /// Prints the amount of memory allocated for the current process in megabytes
        /// </summary>
        private void PrintAllocatedMemory()
        {
            string memoryUnit = "KB";
            var memory = GetAllocatedMemory() / 1024.0;

            if (memory > 1024)
            {
                memory /= 1024;
                memoryUnit = "MB";
            }
            if (memory > 1024)
            {
                memory /= 1024;
                memoryUnit = "GB";
            }

            Console.WriteLine($"Allocated memory: {memory:0.00} {memoryUnit}");
        }

        /// <summary>
        /// Gets the amount of memory allocated for the current process in bytes
        /// </summary>
        /// <returns>
        /// Returns the allocated memory in bytes
        /// </returns>
        private long GetAllocatedMemory()
        {
            var current = Process.GetCurrentProcess();
            if (current == null)
            {
                return 0;
            }

            current.Refresh();
            return current.WorkingSet64;
        }

        /// <summary>
        /// Sends a ping message to the blockchain
        /// </summary>
        /// <param name="height">
        /// The block height to be passed in the ping message
        /// </param>
        private void SendBlockchainPingMessage()
        {
            using (var snapshot = Blockchain.Singleton.GetSnapshot())
            {
                System.LocalNode.Tell(Message.Create(MessageCommand.Ping, PingPayload.Create(snapshot.Height)));
            }
        }
    }
}
