using System.Threading.Tasks;
using Grpc.Core;
using Neo.FileStorage.API.Reputation;
using APIReputationService = Neo.FileStorage.API.Reputation.ReputationService;

namespace Neo.FileStorage.Storage.Services.Reputaion.Service
{
    public class ReputationServiceImpl : APIReputationService.ReputationServiceBase
    {
        public ReputationSignService SignService { get; init; }

        public override Task<AnnounceIntermediateResultResponse> AnnounceIntermediateResult(AnnounceIntermediateResultRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.AnnounceIntermediateResult(request, context.CancellationToken);
            }, context.CancellationToken);
        }
        public override Task<AnnounceLocalTrustResponse> AnnounceLocalTrust(AnnounceLocalTrustRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.AnnounceLocalTrust(request, context.CancellationToken);
            }, context.CancellationToken);
        }
    }
}
