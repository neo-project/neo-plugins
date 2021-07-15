using System.Collections.Generic;
using System.Threading;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using V2Container = Neo.FileStorage.API.Container.Container;

namespace Neo.FileStorage.InnerRing.Services.Audit
{
    public class AuditTask
    {
        public CancellationToken Cancellation;
        public IReporter Reporter;
        public Auditor.Context Auditor;
        public ContainerID ContainerID;
        public V2Container Container;
        public NetMap Netmap;
        public List<List<Node>> ContainerNodes;
        public List<ObjectID> SGList;
    }
}
