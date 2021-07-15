using System.Collections.Concurrent;

namespace Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Storage.Consumers
{
    public class IterationConsumerStorage
    {
        private readonly ConcurrentDictionary<uint, ConsumerStorage> store = new();

        public void Put(IterationTrust t)
        {
            if (!store.TryGetValue(t.Index, out ConsumerStorage storage))
            {
                storage = new();
                store[t.Index] = storage;
            }
            storage.Put(t);
        }

        public bool Consumers(uint iter, out ConsumerStorage storage)
        {
            return store.TryGetValue(iter, out storage);
        }
    }
}
