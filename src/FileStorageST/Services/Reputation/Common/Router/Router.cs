using Neo.FileStorage.API.Netmap;

namespace Neo.FileStorage.Storage.Services.Reputaion.Common.Route
{
    public class Router : IWriterProvider
    {
        public NodeInfo LocalNodeInfo { get; init; }
        public IBuilder RouteBuilder { get; init; }
        public IRemoteWriterProvider RemoteWriterProvider { get; init; }

        public IWriter InitWriter(ICommonContext context)
        {
            RouteContext rtx;
            if (context is not RouteContext)
            {
                rtx = new(context, new());
            }
            else
            {
                rtx = (RouteContext)context;
            }
            return new TrustWriter
            {
                Router = this,
                RouteContext = rtx,
            };
        }
    }
}
