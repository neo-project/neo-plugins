using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Core.Object;
using Neo.FileStorage.Storage.LocalObjectStorage.Engine;

namespace Neo.FileStorage.Storage
{
    public class LocalObjectInhumer : IObjectDeleteHandler
    {
        public StorageEngine LocalStorage { get; init; }

        public void DeleteObjects(params Address[] addresses)
        {
            for (int i = 1; i < addresses.Length; i++)
            {
                LocalStorage.Inhume(addresses[0], addresses[i]);
            }
        }
    }
}
