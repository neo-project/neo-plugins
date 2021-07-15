
using V2Address = Neo.FileStorage.API.Refs.Address;

namespace Neo.FileStorage.Storage.Services.Object.Delete
{
    public interface ITombstoneWriter
    {
        void SetAddress(V2Address address);
    }
}
