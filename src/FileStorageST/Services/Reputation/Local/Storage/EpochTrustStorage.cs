using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Neo.FileStorage.Storage.Services.Reputaion.Local.Storage
{
    public class EpochTrustStorage
    {
        private readonly ConcurrentDictionary<PeerID, TrustValue> store = new();

        public void Update(UpdatePrm prm)
        {
            if (!store.TryGetValue(prm.PeerId, out TrustValue tv))
            {
                tv = new();
                store[prm.PeerId] = tv;
            }
            if (prm.Sat) tv.Sat++;
            tv.All++;
        }


        public void Iterate(Action<Trust> handler)
        {
            double sum = 0;
            Dictionary<PeerID, double> values = new();
            foreach (var (id, tv) in store)
            {
                if (0 < tv.All)
                {
                    double num = (double)tv.Sat;
                    double denom = (double)tv.All;
                    double value = num / denom;
                    values[id] = value;
                    sum += value;
                }
            }
            if (sum != 0)
            {
                foreach (var trust in values.Select(p => new Trust { Peer = p.Key, Value = p.Value / sum }))
                {
                    handler(trust);
                }
            }
        }
    }
}
