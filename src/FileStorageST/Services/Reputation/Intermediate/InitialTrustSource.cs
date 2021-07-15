using System.Linq;
using Neo.FileStorage.Storage.Cache;

namespace Neo.FileStorage.Storage.Services.Reputaion.Intermediate
{
    public class InitialTrustSource
    {
        public NetmapCache NetmapCache { get; init; }

        public double InitialTrust(PeerID peer)
        {
            var nm = NetmapCache.GetNetMap(1);
            if (!nm.Nodes.Any()) return 0;
            return 1.0 / nm.Nodes.Count;
        }
    }
}
