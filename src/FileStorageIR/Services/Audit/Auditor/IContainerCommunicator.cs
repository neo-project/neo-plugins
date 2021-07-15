using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.StorageGroup;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.InnerRing.Services.Audit.Auditor
{
    public interface IContainerCommunicator
    {
        StorageGroup GetStorageGroup(AuditTask task, ObjectID oid);
        FSObject GetHeader(AuditTask task, Node node, ObjectID oid, bool relay);
        byte[] GetRangeHash(AuditTask task, Node node, ObjectID oid, Range range);
    }
}
