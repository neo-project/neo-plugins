using System.Collections.Concurrent;

namespace Neo.FileStorage.Storage.Services.Reputaion.Local.Storage
{
    public class TrustStorage
    {
        private readonly ConcurrentDictionary<ulong, EpochTrustStorage> store = new();

        public void Update(UpdatePrm prm)
        {
            if (store.TryGetValue(prm.Epoch, out EpochTrustStorage storage))
            {
                storage.Update(prm);
                return;
            }
            storage = new();
            storage.Update(prm);
            store[prm.Epoch] = storage;
        }

        public bool DataForEpoch(ulong epoch, out EpochTrustStorage storage)
        {
            return store.TryGetValue(epoch, out storage);
        }
    }
}
