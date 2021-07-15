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
using FSObject = Neo.FileStorage.API.Object.Object;
using FSStorageGroup = Neo.FileStorage.API.StorageGroup.StorageGroup;

namespace Neo.FileStorage.InnerRing.Services.Audit.Auditor
{
    public partial class Context
    {
        private int porRequests;
        private int porRetries;

        private void ExecutePoR()
        {
            Console.WriteLine("ExecutePoR----Step1");
            if (Expired) return;
            List<Task> tasks = new();
            foreach (var (oid, index) in AuditTask.SGList.Select((p, index) => (p, index)))
            {
                Console.WriteLine("ExecutePoR----Step1-1");
                Task t = new(() =>
                {
                    CheckStorageGroupPoR(index, oid);
                });
                Console.WriteLine("ExecutePoR----Step1-2");
                if ((bool)PorPool.Ask(new WorkerPool.NewTask { Process = "POR", Task = t }).Result)
                {
                    tasks.Add(t);
                }
                Console.WriteLine("ExecutePoR----Step1-3");
            }
            Console.WriteLine("ExecutePoR----Step2");
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("ExecutePoR----Step3");
            report.SetPoRCounters((uint)porRequests, (uint)porRetries);
            Console.WriteLine("ExecutePoR----Step4");
        }

        private void CheckStorageGroupPoR(int index, ObjectID oid)
        {
            Console.WriteLine("CheckStorageGroupPoR----Step1");
            FSStorageGroup sg;
            try
            {
                sg = ContainerCommunacator.GetStorageGroup(AuditTask, oid);
            }
            catch (Exception e)
            {
                Console.WriteLine("CheckStorageGroupPoR,error:" + e);
                return;
            }
            Console.WriteLine("CheckStorageGroupPoR----Step2");
            var members = sg.Members.ToList();
            UpdateSGInfo(index, members);
            Console.WriteLine("CheckStorageGroupPoR----Step3");
            int acc_req = 0, acc_retries = 0;
            ulong total_size = 0;
            byte[] tzhash = null;
            Console.WriteLine("CheckStorageGroupPoR----Step4");
            foreach (var member in members)
            {
                Console.WriteLine("CheckStorageGroupPoR----Step4-1");
                List<List<Node>> placement;
                try
                {
                    placement = BuildPlacement(member);
                }
                catch (Exception e)
                {
                    Console.WriteLine("CheckStorageGroupPoR----Step4-1,error:" + e);
                    continue;
                }
                Console.WriteLine("CheckStorageGroupPoR----Step4-2");
                var random = new Random();
                var flat = placement.Flatten().OrderBy(p => random.Next()).ToList();
                Console.WriteLine("CheckStorageGroupPoR----Step4-3");
                for (int j = 0; j < flat.Count; j++)
                {
                    Console.WriteLine("CheckStorageGroupPoR----Step4-3-1");
                    acc_req++;
                    if (0 < j) acc_retries++;
                    FSObject header;
                    try
                    {
                        header = ContainerCommunacator.GetHeader(AuditTask, flat[j], member, true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("CheckStorageGroupPoR----Step4-3-1,error:" + e);
                        continue;
                    }
                    Console.WriteLine("CheckStorageGroupPoR----Step4-3-2");
                    UpdateHeader(header);
                    Console.WriteLine("CheckStorageGroupPoR----Step4-3-3");
                    if (tzhash is null)
                        tzhash = header.Header.HomomorphicHash.Sum.ToByteArray();
                    else
                    {
                        try
                        {
                            tzhash = TzHash.Concat(new List<byte[]> { tzhash, header.Header.HomomorphicHash.Sum.ToByteArray() });
                        }
                        catch
                        {
                            break;
                        }
                    }
                    Console.WriteLine("CheckStorageGroupPoR----Step4-3-4");
                    total_size += header.Header.PayloadLength;
                    break;
                }
            }
            Console.WriteLine("CheckStorageGroupPoR----Step5");
            Interlocked.Add(ref porRequests, acc_req);
            Interlocked.Add(ref porRetries, acc_retries);
            var size_check = sg.ValidationDataSize == total_size;
            var tz_check = tzhash.SequenceEqual(sg.ValidationHash.Sum.ToByteArray());
            Console.WriteLine("CheckStorageGroupPoR----Step6");
            if (size_check && tz_check)
                report.PassedPoR(oid);
            else
            {
                if (!size_check)
                    Utility.Log(nameof(CheckStorageGroupPoR), LogLevel.Debug, $"storage group size check failed, expected={sg.ValidationDataSize}, actual={total_size}");
                else
                    Utility.Log(nameof(CheckStorageGroupPoR), LogLevel.Debug, $"storage group tz hash check failed");
                report.FailedPoR(oid);
            }
            Console.WriteLine("CheckStorageGroupPoR----Step7");
        }

        private void UpdateSGInfo(int index, List<ObjectID> members)
        {
            sgMembersCache[index] = members;
        }
    }
}
