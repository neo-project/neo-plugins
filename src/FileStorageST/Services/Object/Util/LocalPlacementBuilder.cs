using System.Collections.Generic;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Network;
using Neo.FileStorage.Placement;
using FSAddress = Neo.FileStorage.API.Refs.Address;

namespace Neo.FileStorage.Storage.Services.Object.Util
{
    public class LocalPlacementBuilder : IPlacementBuilder
    {
        private readonly IPlacementBuilder builder;
        private readonly Address localAddress;

        public LocalPlacementBuilder(IPlacementBuilder builder, Address address)
        {
            this.builder = builder;
            localAddress = address;
        }

        public List<List<Node>> BuildPlacement(FSAddress address, PlacementPolicy policy)
        {
            var node_list = builder.BuildPlacement(address, policy);
            foreach (var ns in node_list)
            {
                foreach (var n in ns)
                {
                    var addr = Address.FromString(n.NetworkAddress);
                    if (addr == localAddress)
                        return new List<List<Node>> { new List<Node> { n } };
                }
            }
            return null;
        }
    }
}
