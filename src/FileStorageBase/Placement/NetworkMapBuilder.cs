using System.Collections.Generic;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Morph.Invoker;

namespace Neo.FileStorage.Placement
{
    public class NetworkMapBuilder : IPlacementBuilder
    {
        private readonly MorphInvoker invoker;
        private readonly NetMap netMap;

        public NetworkMapBuilder(MorphInvoker invoker)
        {
            this.invoker = invoker;
        }

        public NetworkMapBuilder(NetMap nm)
        {
            netMap = nm;
        }

        public virtual List<List<Node>> BuildPlacement(Address address, PlacementPolicy policy)
        {
            var netmap = GetLatestNetmap();
            var nodes = netmap.GetContainerNodes(policy, address.ContainerId.Value.ToByteArray());
            return BuildObjectPlacement(netmap, nodes, address.ObjectId);
        }

        public static List<List<Node>> BuildObjectPlacement(NetMap netmap, List<List<Node>> container_nodes, ObjectID oid)
        {
            if (oid is null)
                return container_nodes;
            return netmap.GetPlacementVectors(container_nodes, oid.Value.ToByteArray());
        }

        private NetMap GetLatestNetmap()
        {
            if (netMap is not null)
                return netMap;
            return invoker.Snapshot(0);
        }
    }
}
