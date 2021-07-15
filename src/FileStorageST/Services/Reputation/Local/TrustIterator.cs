using System;
using System.Linq;
using Neo.FileStorage.Storage.Services.Reputaion.Common;
using Neo.FileStorage.Storage.Services.Reputaion.Local.Storage;

namespace Neo.FileStorage.Storage.Services.Reputaion.Local
{
    public class TrustIterator : IIterator
    {
        public ICommonContext Context { get; init; }
        public LocalTrustStorage LocalTrustStorage { get; init; }
        public EpochTrustStorage EpochTrustStorage { get; init; }

        public void Iterate(Action<Trust> handler)
        {
            if (EpochTrustStorage is not null)
            {
                EpochTrustStorage.Iterate(handler);
                return;
            }
            var nm = LocalTrustStorage.NetmapCache.GetNetMapByEpoch(Context.Epoch);
            int lindex = -1;
            for (int i = 0; i < nm.Nodes.Count; i++)
                if (nm.Nodes[i].PublicKey.SequenceEqual(LocalTrustStorage.LocalKey))
                    lindex = i;
            int len = nm.Nodes.Count;
            if (0 <= lindex && 0 < nm.Nodes.Count)
                len--;
            double p = 1.0 / len;
            for (int i = 0; i < nm.Nodes.Count; i++)
            {
                if (i == lindex) continue;
                Trust t = new()
                {
                    Peer = nm.Nodes[i].PublicKey,
                    Trusting = LocalTrustStorage.LocalKey,
                    Value = p,
                };
                handler(t);
            }
        }
    }
}
