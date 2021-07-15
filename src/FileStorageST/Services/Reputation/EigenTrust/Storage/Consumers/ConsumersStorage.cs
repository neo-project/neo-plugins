using System.Collections.Concurrent;

namespace Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Storage.Consumers
{
    public class ConsumersStorage
    {
        private readonly ConcurrentDictionary<ulong, IterationConsumerStorage> store = new();

        public void Put(IterationTrust t)
        {
            if (!store.TryGetValue(t.Epoch, out IterationConsumerStorage storage))
            {
                storage = new();
                store[t.Epoch] = storage;
            }
            storage.Put(t);
        }

        public bool Consumers(ulong epoch, uint iter, out ConsumerStorage storage)
        {
            storage = null;
            if (!store.TryGetValue(epoch, out IterationConsumerStorage stor))
            {
                return false;
            }
            return stor.Consumers(iter, out storage);
        }
    }
}
