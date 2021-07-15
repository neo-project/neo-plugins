using Akka.Actor;
using Neo.FileStorage.Morph.Event;
using Neo.FileStorage.Utils;
using Neo.Network.P2P.Payloads;

namespace Neo.FileStorage.InnerRing.Tests.InnerRing.Processors
{
    public class ProcessorFakeActor : ReceiveActor
    {
        public ProcessorFakeActor()
        {
            Receive<Transaction>(create =>
            {
                Sender.Tell(new OperationResult1() { tx = create });
            });
            Receive<WorkerPool.NewTask>(create =>
            {
                Sender.Tell(new OperationResult2() { nt = create });
            });
            Receive<IContractEvent>(create =>
            {
                Sender.Tell(new OperationResult3() { ce = create });
            });
        }

        public class OperationResult1 { public Transaction tx; };
        public class OperationResult2 { public WorkerPool.NewTask nt; };
        public class OperationResult3 { public IContractEvent ce; };
    }
}
