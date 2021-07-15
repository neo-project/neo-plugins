using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Core.Object;
using Neo.FileStorage.Storage.LocalObjectStorage.Engine;

namespace Neo.FileStorage.Storage
{
    public class LocalObjectRemover : IObjectDeleteHandler
    {
        public StorageEngine LocalStorage { get; init; }

        public void DeleteObjects(params Address[] addresses)
        {
            LocalStorage.Delete(addresses);
        }
    }
}
