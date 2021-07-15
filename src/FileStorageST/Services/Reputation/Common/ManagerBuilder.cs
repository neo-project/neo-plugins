using System;
using System.Collections.Generic;
using System.Linq;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Storage.Cache;

namespace Neo.FileStorage.Storage.Services.Reputaion.Common
{
    public class ManagerBuilder
    {
        public NetmapCache NetmapCache { get; init; }

        public List<NodeInfo> BuilderManagers(ulong epoch, PeerID peer)
        {
            var nm = NetmapCache.GetNetMapByEpoch(epoch);
            var nodes = nm.Nodes.OrderBy(p => p.ID.Distance(epoch)).ToArray();
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].PublicKey.SequenceEqual(peer.ToByteArray()))
                {
                    if (i + 1 == nodes.Length)
                        return new() { nodes[0].Info };
                    else
                        return new() { nodes[i].Info };
                }
            }
            return new();
        }
    }
}
