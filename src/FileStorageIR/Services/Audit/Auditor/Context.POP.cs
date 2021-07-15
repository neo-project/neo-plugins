using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Placement;
using Neo.IO.Json;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.InnerRing.Services.Audit.Auditor
{
    public partial class Context
    {
        private readonly ConcurrentDictionary<int, List<ObjectID>> sgMembersCache = new();
        private readonly ConcurrentDictionary<string, List<List<Node>>> placementCache = new();
        private uint hit, miss, fail;
        private int ContainerNodesNumber => AuditTask.ContainerNodes.Flatten().Count;

        private void ExecutePoP()
        {
            if (Expired) return;
            BuildCoverage();
            report.SetPlacementResults(hit, miss, fail);
        }

        private void BuildCoverage()
        {
            var replicas = AuditTask.Container.PlacementPolicy.Replicas;
            foreach (var members in sgMembersCache.Values)
            {
                Random rand = new();
                foreach (var oid in members.OrderBy(p => rand.Next()))
                {
                    var nn = BuildPlacement(oid);
                    for (int i = 0; i < nn.Count && i < replicas.Count; i++)
                    {
                        ProcessObjectPlacement(oid, replicas[i].Count, nn[i]);
                        if (ContainerCovered()) return;
                    }
                }
            }
        }

        private bool ContainerCovered()
        {
            return ContainerNodesNumber <= pairedNodes.Count;
        }

        private void ProcessObjectPlacement(ObjectID oid, uint replicas, List<Node> nodes)
        {
            uint ok = 0;
            bool optimal = false;
            int unpaired_candidate1 = -1, unpaired_candidate2 = -1, paired_candidate = -1;
            for (int i = 0; i < nodes.Count && ok < replicas; i++)
            {
                FSObject header;
                try
                {
                    header = ContainerCommunacator.GetHeader(AuditTask, nodes[i], oid, false);
                }
                catch (Exception)
                {
                    continue;
                }
                UpdateHeader(header);
                ok++;
                optimal = ok == replicas && i < replicas;
                if (ObjectSize(oid) < (ulong)minGamePayloadSize) continue;
                if (!pairedNodes.ContainsKey(nodes[i].Hash))
                {
                    if (unpaired_candidate1 < 0)
                        unpaired_candidate1 = i;
                    else if (unpaired_candidate2 < 0)
                        unpaired_candidate2 = i;
                }
                else if (paired_candidate < 0)
                {
                    paired_candidate = i;
                }
            }
            if (optimal)
                hit++;
            else if (ok == replicas)
                miss++;
            else
                fail++;
            if (0 <= unpaired_candidate1)
            {
                if (0 <= unpaired_candidate2)
                {
                    ComposePair(oid, nodes[unpaired_candidate1], nodes[unpaired_candidate2]);
                }
                else if (0 <= paired_candidate)
                {
                    ComposePair(oid, nodes[unpaired_candidate1], nodes[paired_candidate]);
                }
            }
        }

        private void ComposePair(ObjectID oid, Node n1, Node n2)
        {
            pairs.Add(new GamePair
            {
                N1 = n1,
                N2 = n2,
                Id = oid,
            });
            pairedNodes[n1.Hash] = new PairMemberInfo
            {
                Node = n1,
            };
            pairedNodes[n2.Hash] = new PairMemberInfo
            {
                Node = n2,
            };
        }

        private List<List<Node>> BuildPlacement(ObjectID oid)
        {
            if (placementCache.TryGetValue(oid.ToBase58String(), out List<List<Node>> table))
            {
                return table;
            }
            var nn = NetworkMapBuilder.BuildObjectPlacement(AuditTask.Netmap, AuditTask.ContainerNodes, oid);
            Console.WriteLine($"placement for {oid.ToBase58String()}: {new JArray(nn.Select(p => new JArray(p.Select(q => q.ToJson()))))}");
            placementCache[oid.ToBase58String()] = nn;
            return nn;
        }
    }
}
