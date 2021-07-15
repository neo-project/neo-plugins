using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Storage.Services.Container.Announcement.Route.Placement;
using System;
using System.Threading;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route
{
    public class LoadRouter : IWriterProvider
    {
        public RemoteLoadAnnounceProvider RemoteProvider { get; init; }
        public RouteBuilder RouteBuilder { get; init; }
        public NodeInfo LocalNodeInfo { get; init; }

        public IWriter InitWriter(CancellationToken cancellation)
        {
            RouteContext context = new(new() { LocalNodeInfo }, cancellation);
            return new LoadWriter()
            {
                Router = this,
                RouteContext = context,
            };
        }

        public LoadWriter InitWriter(RouteContext context)
        {
            return new LoadWriter()
            {
                Router = this,
                RouteContext = context,
            };
        }
    }
}
