using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.Storage.Services.Object.Delete.Writer
{
    public class SimpleTombstoneWriter : ITombstoneWriter
    {
        public DeleteResponse Response { get; init; }

        public void SetAddress(Address address)
        {
            Response.Body = new()
            {
                Tombstone = address,
            };
        }
    }
}
