using System;
using Neo.FileStorage.API.Object;

namespace Neo.FileStorage.Storage.Services.Object.Get.Execute
{
    public partial class ExecuteContext
    {
        private bool ProcessNode(Network.Address address)
        {
            var client = GetService.ClientCache.Get(address);
            try
            {
                collectedObject = client.GetObject(this);
                WriteCollectedObject();
                return true;
            }
            catch (Exception e) when (e is not SplitInfoException) //TODO: || is not already removed
            {
                return false;
            }
        }
    }
}
