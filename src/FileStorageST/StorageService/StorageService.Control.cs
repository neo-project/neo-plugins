using System;
using Neo.FileStorage.Storage.LocalObjectStorage.Engine;
using Neo.FileStorage.Storage.Services.Control;

namespace Neo.FileStorage.Storage
{
    public sealed partial class StorageService : IDisposable
    {
        private ControlServiceImpl InitializeControl()
        {
            return new ControlServiceImpl
            {
                Key = key,
                LocalStorage = localStorage,
                MorphInvoker = morphInvoker,
                StorageNode = this,
            };
        }
    }
}
