using System;
using Neo.FileStorage.Storage.Services.Accounting;

namespace Neo.FileStorage.Storage
{
    public sealed partial class StorageService : IDisposable
    {
        private AccountingServiceImpl InitializeAccounting()
        {
            return new AccountingServiceImpl
            {
                SignService = new()
                {
                    Key = key,
                    ResponseService = new()
                    {
                        StorageNode = this,
                        AccountingService = new()
                        {
                            MorphInvoker = morphInvoker,
                        }
                    }
                }
            };
        }
    }
}
