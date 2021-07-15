using Neo.FileStorage.API.Netmap;
using System.Collections.Generic;
using System.Threading;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route
{
    public class RouteContext
    {
        public CancellationToken Cancellation;
        public List<NodeInfo> PassedRoute;

        public RouteContext(List<NodeInfo> passed, CancellationToken cancellation)
        {
            PassedRoute = passed;
            Cancellation = cancellation;
        }
    }
}
