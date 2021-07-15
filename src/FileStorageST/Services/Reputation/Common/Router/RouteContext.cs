using System.Collections.Generic;
using System.Threading;
using Neo.FileStorage.API.Netmap;

namespace Neo.FileStorage.Storage.Services.Reputaion.Common.Route
{
    public class RouteContext : ICommonContext
    {
        public CancellationToken Cancellation { get; private set; }
        public ulong Epoch { get; private set; }
        public List<NodeInfo> Passed { get; private set; }

        public RouteContext(ICommonContext common, List<NodeInfo> passed)
        {
            Cancellation = common.Cancellation;
            Epoch = common.Epoch;
            Passed = passed;
        }
    }
}
