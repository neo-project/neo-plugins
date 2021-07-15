using Neo.FileStorage.Placement;
using FSAddress = Neo.FileStorage.API.Refs.Address;

namespace Neo.FileStorage.Storage.Services.Object.Util
{
    public interface ITraverserGenerator
    {
        Traverser GenerateTraverser(FSAddress address, ulong epoch);
    }
}
