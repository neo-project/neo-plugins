namespace Neo.FileStorage.Storage.Services.Object.Search.Remote
{
    public interface ISearchClientCache
    {
        ISearchClient Get(Network.Address address);
    }
}
