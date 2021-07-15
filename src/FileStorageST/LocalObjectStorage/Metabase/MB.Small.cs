using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.LocalObjectStorage.Blob;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Metabase
{
    public sealed partial class MB
    {
        public BlobovniczaID IsSmall(Address address)
        {
            return db.Get(SmallKey(address));
        }
    }
}
