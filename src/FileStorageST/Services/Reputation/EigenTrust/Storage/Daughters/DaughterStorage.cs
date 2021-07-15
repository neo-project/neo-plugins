using System;
using System.Collections.Concurrent;

namespace Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Storage.Daughters
{
    public class DaughterStorage
    {
        private readonly ConcurrentDictionary<PeerID, DaughterTrusts> store = new();

        public void Put(Trust t)
        {
            if (!store.TryGetValue(t.Trusting, out DaughterTrusts storage))
            {
                storage = new();
                store[t.Trusting] = storage;
            }
            storage.Put(t);
        }

        public void Iterate(Action<PeerID, DaughterTrusts> handler)
        {
            foreach (var (key, value) in store)
            {
                try
                {
                    handler(key, value);
                }
                catch
                {
                    break;
                }
            }
        }

        public bool DaughterTrusts(PeerID peer, out DaughterTrusts trusts)
        {
            return store.TryGetValue(peer, out trusts);
        }
    }
}
