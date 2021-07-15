using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Neo.FileStorage.API.Cryptography.Tz;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Utils;
using static Neo.FileStorage.InnerRing.Services.Audit.Auditor.Helper;
using FSRange = Neo.FileStorage.API.Object.Range;

namespace Neo.FileStorage.InnerRing.Services.Audit.Auditor
{
    public partial class Context
    {
        private void ExecutePDP()
        {
            if (Expired) return;
            ProcessPairs();
            WritePairsResult();
        }

        private void ProcessPairs()
        {
            List<Task> tasks = new();
            foreach (var pair in pairs)
            {
                Task t = new(() =>
                {
                    ProcessPair(pair);
                });
                if ((bool)PorPool.Ask(new WorkerPool.NewTask { Process = "PDP", Task = t }).Result)
                {
                    tasks.Add(t);
                }
            }
            Task.WaitAll(tasks.ToArray());
        }

        private void ProcessPair(GamePair pair)
        {
            DistributeRanges(pair);
            CollectHashes(pair);
            AnalyzeHashes(pair);
        }

        private void WritePairsResult()
        {
            List<byte[]> passed = new(), failed = new();
            foreach (var info in pairedNodes)
            {
                if (info.Value.State)
                    passed.Add(info.Value.Node.PublicKey);
                else
                    failed.Add(info.Value.Node.PublicKey);
            }
            report.SetPDPResults(passed, failed);
        }

        private void DistributeRanges(GamePair pair)
        {
            pair.Range1 = new List<FSRange>(hashRangeNumber - 1);
            pair.Range2 = new List<FSRange>(hashRangeNumber - 1);
            for (int i = 0; i < hashRangeNumber - 1; i++)
            {
                pair.Range1.Add(new FSRange());
                pair.Range2.Add(new FSRange());
            }
            var notches = SplitPayload(pair.Id);

            pair.Range1[0].Offset = 0;
            pair.Range1[0].Length = notches[1];
            pair.Range1[1].Offset = notches[1];
            pair.Range1[1].Length = notches[2] - notches[1];
            pair.Range1[2].Offset = notches[2];
            pair.Range1[2].Length = notches[3] - notches[2];

            pair.Range2[0].Offset = 0;
            pair.Range2[0].Length = notches[0];
            pair.Range2[1].Offset = notches[0];
            pair.Range2[1].Length = notches[1] - notches[0];
            pair.Range2[2].Offset = notches[1];
            pair.Range2[2].Length = notches[3] - notches[1];
        }

        private void CollectHashes(GamePair pair)
        {
            pair.Hashes1 = new List<byte[]>(pair.Range1.Count);
            foreach (var range in pair.Range1)
            {
                Thread.Sleep((int)RandomUInt64(MaxPDPInterval));
                try
                {
                    var hash = ContainerCommunacator.GetRangeHash(AuditTask, pair.N1, pair.Id, range);
                    pair.Hashes1.Add(hash);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"node1 rang hash failed. range={range}. error={e}");
                    return;
                }
            }
            pair.Hashes2 = new List<byte[]>(pair.Range2.Count);
            foreach (var range in pair.Range2)
            {
                Thread.Sleep((int)RandomUInt64(MaxPDPInterval));
                try
                {
                    var hash = ContainerCommunacator.GetRangeHash(AuditTask, pair.N2, pair.Id, range);
                    pair.Hashes2.Add(hash);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"node2 rang hash failed. range={range}. error={e}");
                    return;
                }
            }
        }

        private void AnalyzeHashes(GamePair pair)
        {
            Console.WriteLine("AnalyzeHashes--step1");
            if (pair.Hashes1.Count != hashRangeNumber - 1 || pair.Hashes2.Count != hashRangeNumber - 1)
            {
                FailNodes(pair.N1, pair.N2);
                return;
            }
            Console.WriteLine("AnalyzeHashes--step2");
            var h1 = TzHash.Concat(new List<byte[]>() { pair.Hashes2[0], pair.Hashes2[1] });
            if (h1 is null || !pair.Hashes1[0].SequenceEqual(h1))
            {
                FailNodes(pair.N1, pair.N2);
                return;
            }
            Console.WriteLine("AnalyzeHashes--step3");
            var h2 = TzHash.Concat(new List<byte[]>() { pair.Hashes1[1], pair.Hashes1[2] });
            if (h2 is null || !pair.Hashes2[2].SequenceEqual(h2))
            {
                FailNodes(pair.N1, pair.N2);
                return;
            }
            Console.WriteLine("AnalyzeHashes--step4");
            var fh = TzHash.Concat(new List<byte[]>() { h1, h2 });
            var expected = ObjectHomoHash(pair.Id);
            if (fh is null || expected is null || !expected.SequenceEqual(fh))
            {
                FailNodes(pair.N1, pair.N2);
                return;
            }
            Console.WriteLine("AnalyzeHashes--step5");
            PassNodesPDP(pair.N1, pair.N2);
        }

        private List<ulong> SplitPayload(ObjectID oid)
        {
            ulong prev = 0, size = ObjectSize(oid);
            List<ulong> notches = new();
            for (int i = 0; i < hashRangeNumber; i++)
            {
                ulong next_len;
                if (i < hashRangeNumber - 1)
                    next_len = RandomUInt64(size - prev - (hashRangeNumber - (ulong)i)) + 1;
                else
                    next_len = size - prev;
                notches.Add(prev + next_len);
                prev += next_len;
            }
            return notches;
        }

        private void FailNodes(params Node[] nodes)
        {
            foreach (var node in nodes)
                pairedNodes[node.Hash] = new PairMemberInfo
                {
                    State = false,
                    Node = node,
                };
        }

        private byte[] ObjectHomoHash(ObjectID oid)
        {
            if (HeaderCache.TryGetValue(oid.ToBase58String(), out ShortHeader header))
            {
                return header.TzHash;
            }
            return null;
        }

        private void PassNodesPDP(params Node[] nodes)
        {
            foreach (var node in nodes)
                pairedNodes[node.Hash] = new PairMemberInfo
                {
                    State = true,
                    Node = node,
                };
        }
    }
}
