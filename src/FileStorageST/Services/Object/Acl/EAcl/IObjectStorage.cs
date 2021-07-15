using Neo.FileStorage.API.Refs;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Acl.EAcl
{
    public interface ILocalHeadSource
    {
        FSObject Head(Address address);
    }
}
