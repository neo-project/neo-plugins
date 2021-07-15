using Neo.FileStorage.Cache;
using Neo.FileStorage.Storage.Services.Object.Search.Remote;

namespace Neo.FileStorage.Storage.Services.Object.Search.Clients
{
    public class SearchClientCache : ISearchClientCache
    {
        private readonly IFSClientCache clientCache;

        public SearchClientCache(IFSClientCache cache)
        {
            clientCache = cache;
        }

        ISearchClient ISearchClientCache.Get(Network.Address address)
        {
            return new SearchClient(clientCache.Get(address));
        }
    }
}
