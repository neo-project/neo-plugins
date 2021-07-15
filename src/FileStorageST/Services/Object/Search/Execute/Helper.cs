using System.Collections.Generic;
using System.Linq;
using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.Storage.Services.Object.Search.Execute
{
    public static class Helper
    {
        public static IEnumerable<ObjectID> Search(this ILocalSearchSource engine, ExecuteContext context)
        {
            return engine.Select(context.Prm.ContainerID, context.Prm.Filters)?.Select(p => p.ObjectId);
        }
    }
}
