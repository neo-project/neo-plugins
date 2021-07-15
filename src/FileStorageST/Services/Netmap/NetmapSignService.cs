using Neo.FileStorage.API.Netmap;

namespace Neo.FileStorage.Storage.Services.Netmap
{
    public class NetmapSignService : SignService
    {
        public NetmapResponseService ResponseService { get; init; }

        public LocalNodeInfoResponse LocalNodeInfo(LocalNodeInfoRequest request)
        {
            return (LocalNodeInfoResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.LocalNodeInfo((LocalNodeInfoRequest)r);
            });
        }

        public NetworkInfoResponse NetworkInfo(NetworkInfoRequest request)
        {
            return (NetworkInfoResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.NetworkInfo((NetworkInfoRequest)r);
            });
        }
    }
}
