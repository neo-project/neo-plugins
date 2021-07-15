using Grpc.Core;
using Neo.FileStorage.API.Netmap;
using System.Threading.Tasks;
using APINetmapService = Neo.FileStorage.API.Netmap.NetmapService;

namespace Neo.FileStorage.Storage.Services.Netmap
{
    public class NetmapServiceImpl : APINetmapService.NetmapServiceBase
    {
        public NetmapSignService SignService { get; init; }

        /// <summary>
        /// Get NodeInfo structure from the particular node directly. Node information
        /// can be taken from `Netmap` smart contract, but in some cases the one may
        /// want to get recent information directly, or to talk to the node not yet
        /// present in `Network Map` to find out what API version can be used for
        /// further communication. Can also be used to check if node is up and running.
        /// </summary>
        /// <param name="request">The request received from the client.</param>
        /// <param name="context">The context of the server-side call handler being invoked.</param>
        /// <returns>The response to send back to the client (wrapped by a task).</returns>
        public override Task<LocalNodeInfoResponse> LocalNodeInfo(LocalNodeInfoRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.LocalNodeInfo(request);
            }, context.CancellationToken);
        }

        /// <summary>
        /// Read recent information about the NeoFS network.
        /// </summary>
        /// <param name="request">The request received from the client.</param>
        /// <param name="context">The context of the server-side call handler being invoked.</param>
        /// <returns>The response to send back to the client (wrapped by a task).</returns>
        public override Task<NetworkInfoResponse> NetworkInfo(NetworkInfoRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.NetworkInfo(request);
            }, context.CancellationToken);
        }
    }
}
