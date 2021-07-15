using System;
using System.Threading.Tasks;
using Akka.Actor;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Storage.Services.Reputaion.Common.Route;
using Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Storage.Consumers;
using Neo.FileStorage.Storage.Services.Reputaion.Intermediate;
using Neo.FileStorage.Utils;

namespace Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Calculate
{
    public class Calculator
    {
        public MorphInvoker MorphInvoker { get; init; }
        public InitialTrustSource InitialTrustSource { get; init; }
        public DaughterTrustIteratorProvider DaughterTrustSource { get; init; }
        public Router IntermediateValueTarget { get; init; }
        public FinalWriterProvider FinalWriteTarget { get; init; }
        public IActorRef WorkerPool { get; init; }
        private double alpha;
        private double beta;

        public void Calculate(IterationContext context)
        {
            alpha = MorphInvoker.EigenTrustAlpha();
            beta = 1 - alpha;
            if (context.Index == 0)
            {
                SendInitValues(context);
                return;
            }
            --context.Index;
            var consumers = DaughterTrustSource.InitConsumerIterator(context);
            ++context.Index;
            consumers.Iterate((p, trusts) =>
            {
                WorkerPool.Tell(new WorkerPool.NewTask
                {
                    Process = "Reputation",
                    Task = new(() =>
                    {
                        IterateDaughter(context, p, trusts, context.Last);
                    }),
                });
            });
        }

        private void SendInitValues(IterationContext context)
        {
            try
            {
                var daughters = DaughterTrustSource.InitAllDaughtersIterator(context);
                var intermediate_writer = IntermediateValueTarget.InitWriter(context);
                daughters.Iterate((p, trusts) =>
                {
                    trusts.Iterate(t =>
                    {
                        try
                        {
                            var init_t = InitialTrustSource.InitialTrust(p);
                            t.Value *= init_t;
                            intermediate_writer.Write(t);
                        }
                        catch
                        {
                            return;
                        }
                    });
                });
                intermediate_writer.Close();
            }
            catch
            {
                return;
            }
        }

        private void IterateDaughter(IterationContext context, PeerID peer, ConsumerTrusts trusts, bool last)
        {
            var init = InitialTrustSource.InitialTrust(peer);
            var daughters = DaughterTrustSource.InitDaughterIterator(context, peer);
            double sum = 0;
            try
            {
                trusts.Iterate(t =>
                {
                    if (!last && context.Cancellation.IsCancellationRequested)
                        throw new InvalidOperationException();
                    sum += t.Value;
                });
            }
            catch
            {
                return;
            }
            init *= alpha;
            sum *= beta;
            IterationTrust it = new()
            {
                Epoch = context.Epoch,
                Index = context.Index,
                Trust = new()
                {
                    Peer = peer,
                }
            };
            if (last)
            {
                var final_writer = FinalWriteTarget.InitIntermediateWriter();
                it.Trust.Value = sum;
                final_writer.WriteIntermediateTrust(it);
            }
            else
            {
                try
                {
                    var intermediate_writer = IntermediateValueTarget.InitWriter(context);
                    daughters.Iterate(t =>
                    {
                        if (context.Cancellation.IsCancellationRequested)
                            throw new InvalidOperationException();
                        t.Value *= sum;
                        intermediate_writer.Write(t);
                    });
                    intermediate_writer.Close();
                }
                catch
                {
                    return;
                }
            }
        }
    }
}
