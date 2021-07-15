using System;
using System.Collections.Concurrent;

namespace Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Storage.Daughters
{
    public class DaughterTrusts
    {
        private readonly ConcurrentDictionary<PeerID, Trust> store = new();

        public void Put(Trust t)
        {
            store[t.Peer] = t;
        }

        public void Iterate(Action<Trust> handler)
        {
            foreach (var t in store.Values)
                handler(t);
        }
    }
}
