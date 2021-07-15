using System;
using System.Collections.Generic;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Network;
using Neo.FileStorage.Placement;
using FSAddress = Neo.FileStorage.API.Refs.Address;

namespace Neo.FileStorage.Storage.Services.Object.Util
{
    public class RemotePlacementBuilder : NetworkMapBuilder
    {
        private readonly Address localAddress;

        public RemotePlacementBuilder(MorphInvoker invoker, Address address)
        : base(invoker)
        {
            localAddress = address;
        }

        public override List<List<Node>> BuildPlacement(FSAddress address, PlacementPolicy policy)
        {
            var node_list = base.BuildPlacement(address, policy);
            foreach (var ns in node_list)
            {
                foreach (var n in ns)
                {
                    Address addr;
                    try
                    {
                        addr = Address.FromString(n.NetworkAddress);
                    }
                    catch (Exception e)
                    {
                        Utility.Log(nameof(RemotePlacementBuilder), LogLevel.Error, e.Message);
                        continue;
                    }
                    if (addr == localAddress)
                        ns.Remove(n);
                }
            }
            return null;
        }
    }
}
