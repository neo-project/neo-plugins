using Neo.FileStorage.Storage.Services.Reputaion.Common;
using Neo.FileStorage.Storage.Services.Reputaion.EigenTrust;
using Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Storage.Consumers;

namespace Neo.FileStorage.Storage.Services.Reputaion.Intermediate
{
    public class ConsumerStorageWriter : IWriter
    {
        public IterationContext Context { get; init; }
        public ConsumersStorage Storage { get; init; }

        public void Write(Trust trust)
        {
            IterationTrust t = new()
            {
                Epoch = Context.Epoch,
                Index = Context.Index,
                Trust = trust,
            };
            Storage.Put(t);
        }

        public void Close() { }
    }
}
