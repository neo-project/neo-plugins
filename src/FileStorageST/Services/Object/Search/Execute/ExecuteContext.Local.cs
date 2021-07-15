namespace Neo.FileStorage.Storage.Services.Object.Search.Execute
{
    public partial class ExecuteContext
    {
        private void ExecuteLocal()
        {
            Prm.Writer.WriteIDs(SearchService.LocalStorage.Search(this));
        }
    }
}
