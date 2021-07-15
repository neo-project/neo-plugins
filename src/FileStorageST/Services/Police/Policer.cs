using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Storage.Services.Object.Head;
using Neo.FileStorage.Storage.Services.Replicate;
using FSAddress = Neo.FileStorage.API.Refs.Address;

namespace Neo.FileStorage.Storage.Services.Police
{
    public class Policer : UntypedActor
    {
        public class Trigger { }
        private int workScope;
        private readonly Configuration config;
        private readonly PoliceTask prevTask = new();

        public Policer(Configuration c)
        {
            config = c;
            workScope = config.WorkScope;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Trigger _:
                    OnTrigger();
                    break;
            }
        }

        private void OnTrigger()
        {
            prevTask.Cancellation.Cancel();
            HandleTask();
        }

        public void HandleTask()
        {
            int delta;
            if (0 < prevTask.Undone)
                delta = -prevTask.Undone;
            else
                delta = workScope * config.ExpandRate / 100;
            var addrs = Select(workScope + delta);
            if (workScope + delta < addrs.Count)
                workScope += delta;
            prevTask.Undone = addrs.Count;
            prevTask.Cancellation = new CancellationTokenSource();
            prevTask.Task = Task.Run(() =>
            {
                foreach (var addr in addrs)
                {
                    ProcessObject(addr);
                    prevTask.Undone--;
                }
            }, prevTask.Cancellation.Token);
        }

        private List<FSAddress> Select(int limit)
        {
            var res = config.LocalStorage.List((ulong)limit);
            return res.Take(limit).ToList();
        }

        private void ProcessNodes(FSAddress address, List<Node> nodes, uint shortage)
        {
            RemoteHeadPrm prm = new()
            {
                Address = address,
            };
            bool redundantLocalCopy = false;
            for (int i = 0; i < nodes.Count; i++)
            {
                Network.Address node;
                try
                {
                    node = Network.Address.FromString(nodes[i].NetworkAddress);
                }
                catch (Exception)
                {
                    continue;
                }
                if (node.Equals(config.LocalAddress))
                {
                    if (shortage == 0)
                        redundantLocalCopy = true;
                    else
                        shortage--;
                }
                else if (0 < shortage)
                {
                    prm.Node = node;
                    CancellationTokenSource source = new(config.HeadTimeout);
                    try
                    {
                        _ = config.RemoteHeader.Head(prm, source.Token);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    shortage--;
                }
                nodes.RemoveAt(i);
                i--;
            }
            if (0 < shortage)
            {
                config.ReplicatorRef.Tell(new Replicator.Task
                {
                    Quantity = shortage,
                    Address = address,
                    Nodes = nodes,
                });
            }
            else if (redundantLocalCopy)
            {
                config.RedundantCopyCallback(address);
            }
        }

        private void ProcessObject(FSAddress address)
        {
            var container = config.MorphInvoker.GetContainer(address.ContainerId)?.Container;
            var policy = container.PlacementPolicy;
            var nodes = config.PlacementBuilder.BuildPlacement(address, policy);
            var replicas = policy.Replicas;
            for (int i = 0; i < nodes.Count; i++)
                ProcessNodes(address, nodes[i], replicas[i].Count);
        }

        public static Props Props(Configuration c)
        {
            return Akka.Actor.Props.Create(() => new Policer(c));
        }
    }
}
