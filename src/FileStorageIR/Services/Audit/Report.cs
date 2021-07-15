using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Neo.FileStorage.API.Audit;
using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.InnerRing.Services.Audit
{
    public class Report
    {
        private readonly DataAuditResult auditResult;

        public Report()
        {
            auditResult = new DataAuditResult();
            auditResult.Version = Version.SDKVersion();
        }

        public void SetContainerID(ContainerID cid)
        {
            auditResult.ContainerId = cid;
        }

        public void SetPDPResults(List<byte[]> passed, List<byte[]> failed)
        {
            auditResult.PassNodes.AddRange(passed.Select(p => ByteString.CopyFrom(p)));
            auditResult.FailNodes.AddRange(failed.Select(p => ByteString.CopyFrom(p)));
        }

        public void SetPlacementResults(uint hit, uint miss, uint fail)
        {
            auditResult.Hit = hit;
            auditResult.Miss = miss;
            auditResult.Fail = fail;
        }

        public void PassedPoR(ObjectID oid)
        {
            auditResult.PassSg.Add(oid);
        }

        public void FailedPoR(ObjectID oid)
        {
            auditResult.FailSg.Add(oid);
        }

        public void SetPoRCounters(uint requests, uint retries)
        {
            auditResult.Requests = requests;
            auditResult.Retries = retries;
        }

        public void SetComplete()
        {
            auditResult.Complete = true;
        }

        public DataAuditResult Result()
        {
            return auditResult;
        }
    }
}
