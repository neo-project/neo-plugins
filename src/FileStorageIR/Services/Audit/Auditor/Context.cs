using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Akka.Actor;
using Neo.FileStorage.API.Cryptography.Tz;
using Neo.FileStorage.API.Refs;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.InnerRing.Services.Audit.Auditor
{
    public partial class Context
    {
        private const int hashRangeNumber = 4;
        private readonly int minGamePayloadSize = hashRangeNumber * TzHash.TzHashLength;
        public IContainerCommunicator ContainerCommunacator;
        public AuditTask AuditTask;
        public IActorRef PorPool { get; init; }
        public IActorRef PdpPool { get; init; }
        public ulong MaxPDPInterval;//MillisecondsTimeout
        private Report report;
        private readonly ConcurrentDictionary<string, ShortHeader> HeaderCache = new();
        private readonly List<GamePair> pairs = new();
        private readonly ConcurrentDictionary<ulong, PairMemberInfo> pairedNodes = new();
        private bool Expired => AuditTask.Cancellation.IsCancellationRequested;

        public void Execute()
        {
            Console.WriteLine("Audit Execute---step1");
            Initialize();
            Console.WriteLine("Audit Execute---step2");
            ExecutePoR();
            Console.WriteLine("Audit Execute---step3");
            ExecutePoP();
            Console.WriteLine("Audit Execute---step4");
            ExecutePDP();
            Console.WriteLine("Audit Execute---step5");
            Complete();
            Console.WriteLine("Audit Execute---step6");
            WriteReport();
            Console.WriteLine("Audit Execute---step7");
        }

        private void Initialize()
        {
            report = new Report();
            report.SetContainerID(AuditTask.ContainerID);
        }

        private void Complete()
        {
            if (Expired) return;
            report.SetComplete();
        }

        private void WriteReport()
        {
            AuditTask.Reporter.WriteReport(report);
        }

        private ulong ObjectSize(ObjectID oid)
        {
            if (HeaderCache.TryGetValue(oid.ToBase58String(), out ShortHeader header))
                return header.ObjectSize;
            return 0;
        }

        private void UpdateHeader(FSObject header)
        {
            HeaderCache[header.ObjectId.ToBase58String()] = new ShortHeader
            {
                TzHash = header.Header.HomomorphicHash.Sum.ToByteArray(),
                ObjectSize = header.PayloadSize,
            };
        }
    }
}
