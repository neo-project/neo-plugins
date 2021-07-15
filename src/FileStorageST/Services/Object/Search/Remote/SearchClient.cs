using System.Collections.Generic;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.Services.Object.Search.Execute;

namespace Neo.FileStorage.Storage.Services.Object.Search.Remote
{
    public class SearchClient : ISearchClient
    {
        private readonly IFSClient client;

        public SearchClient(IFSClient c)
        {
            client = c;
        }

        IEnumerable<ObjectID> ISearchClient.SearchObjects(ExecuteContext context)
        {
            if (context.Prm.Forwarder is not null)
            {
                return context.Prm.Forwarder(client.Raw());
            }
            return client.SearchObject(
                context.Prm.ContainerID,
                context.Prm.Filters,
                new()
                {
                    Epoch = context.Prm.NetmapEpoch,
                    Key = context.Prm.Key,
                },
                context.Cancellation).Result;
        }
    }
}
