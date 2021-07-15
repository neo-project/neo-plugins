using Neo.FileStorage.API.Netmap;

namespace Neo.FileStorage.Storage.Services.Netmap
{
    public class NetmapResponseService : ResponseService
    {
        public NetmapService NetmapService { get; init; }

        public LocalNodeInfoResponse LocalNodeInfo(LocalNodeInfoRequest request)
        {
            return (LocalNodeInfoResponse)HandleUnaryRequest(request, r =>
            {
                return NetmapService.LocalNodeInfo((LocalNodeInfoRequest)r);
            });
        }

        public NetworkInfoResponse NetworkInfo(NetworkInfoRequest request)
        {
            return (NetworkInfoResponse)HandleUnaryRequest(request, r =>
            {
                return NetmapService.NetworkInfo((NetworkInfoRequest)r);
            });
        }
    }
}
