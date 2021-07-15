using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.Storage.Services.Netmap
{
    public class NetmapService
    {
        public StorageService StorageNode { get; init; } //TODO: if this the best way?
        public LocalNodeInfoResponse LocalNodeInfo(LocalNodeInfoRequest _)
        {
            var resp = new LocalNodeInfoResponse()
            {
                Body = new LocalNodeInfoResponse.Types.Body
                {
                    Version = Version.SDKVersion(),
                    NodeInfo = StorageNode.LocalNodeInfo,
                },
            };
            return resp;
        }

        public NetworkInfoResponse NetworkInfo(NetworkInfoRequest _)
        {
            var resp = new NetworkInfoResponse()
            {
                Body = new NetworkInfoResponse.Types.Body
                {
                    NetworkInfo = new()
                    {
                        CurrentEpoch = StorageNode.CurrentEpoch,
                        MagicNumber = StorageNode.ProtocolSettings.Network,
                    }
                }
            };
            return resp;
        }
    }
}
