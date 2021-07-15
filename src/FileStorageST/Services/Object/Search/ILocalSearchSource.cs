using System.Collections.Generic;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.Storage.Services.Object.Search
{
    public interface ILocalSearchSource
    {
        List<Address> Select(ContainerID cid, SearchFilters filters);
    }
}
