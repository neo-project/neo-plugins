using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using System.Collections.Generic;

namespace Neo.FileStorage.Placement
{
    public interface IPlacementBuilder
    {
        List<List<Node>> BuildPlacement(Address address, PlacementPolicy policy);
    }
}
