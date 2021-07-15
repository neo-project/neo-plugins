namespace Neo.FileStorage.Storage.Services.Object.Search.Execute
{
    public partial class ExecuteContext
    {
        private void ProcessNode(Network.Address address)
        {
            var client = SearchService.ClientCache.Get(address);
            var ids = client.SearchObjects(this);
            Prm.Writer.WriteIDs(ids);
        }
    }
}
