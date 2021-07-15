using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.Storage.Services.Object.Acl.EAcl
{
    public class ValidateUnit
    {
        public ContainerID ContainerId;
        public Role Role;
        public Operation Op;
        public IHeaderSource HeaderSource;
        public byte[] Key;
        public BearerToken Bearer;
    }
}
