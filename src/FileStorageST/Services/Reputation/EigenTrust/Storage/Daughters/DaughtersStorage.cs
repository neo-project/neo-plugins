using System.Collections.Concurrent;

namespace Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Storage.Daughters
{
    public class DaughtersStorage
    {
        private readonly ConcurrentDictionary<ulong, DaughterStorage> store = new();

        public void Put(ulong epoch, Trust t)
        {
            if (!store.TryGetValue(epoch, out DaughterStorage storage))
            {
                storage = new();
                store[epoch] = storage;
            }
            storage.Put(t);
        }

        public bool DaughterTrusts(ulong epoch, PeerID peer, out DaughterTrusts storage)
        {
            storage = null;
            if (!store.TryGetValue(epoch, out DaughterStorage stor))
            {
                return false;
            }
            return stor.DaughterTrusts(peer, out storage);
        }

        public bool AllDaughterTrusts(ulong epoch, out DaughterStorage storage)
        {
            return store.TryGetValue(epoch, out storage);
        }
    }
}
