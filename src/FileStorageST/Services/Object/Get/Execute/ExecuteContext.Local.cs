namespace Neo.FileStorage.Storage.Services.Object.Get.Execute
{
    public partial class ExecuteContext
    {
        private void ExecuteLocal()
        {
            collectedObject = GetService.LocalStorage.Get(Prm.Address);
            WriteCollectedObject();
        }
    }
}
