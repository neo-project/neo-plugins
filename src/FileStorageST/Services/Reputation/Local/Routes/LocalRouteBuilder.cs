using System.Collections.Generic;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Storage.Services.Reputaion.Common;

namespace Neo.FileStorage.Storage.Services.Reputaion.Local.Routes
{
    public class LocalRouteBuilder : IBuilder
    {
        public ManagerBuilder ManagerBuilder { get; init; }

        public List<NodeInfo> NextStage(ulong epoch, Trust trust, List<NodeInfo> passed)
        {
            if (1 < passed.Count) return new();
            return ManagerBuilder.BuilderManagers(epoch, trust.Peer);
        }
    }
}
