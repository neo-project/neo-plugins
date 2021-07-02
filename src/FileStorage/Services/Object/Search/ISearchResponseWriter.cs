
using System.Collections.Generic;
using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.Services.Object.Search
{
    public interface ISearchResponseWriter
    {
        void WriteIDs(IEnumerable<ObjectID> ids);
    }
}