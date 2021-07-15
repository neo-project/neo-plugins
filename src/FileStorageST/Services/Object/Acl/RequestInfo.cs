using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.Session;
using AclOperation = Neo.FileStorage.API.Acl.Operation;

namespace Neo.FileStorage.Storage.Services.Object.Acl
{
    public class RequestInfo
    {
        public uint BasicAcl;
        public Role Role;
        public bool IsInnerRing;
        public AclOperation Op;
        public OwnerID Owner;
        public ContainerID ContainerID;
        public ObjectID ObjectID;
        public byte[] SenderKey;
        public SessionToken OriginalSessionToken;
        public BearerToken Bearer;
        public IRequest Request;

        public Address Address => new() { ContainerId = ContainerID, ObjectId = ObjectID };
    }
}
