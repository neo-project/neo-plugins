using System.Threading;
using Neo.FileStorage.API.Reputation;

namespace Neo.FileStorage.Storage.Services.Reputaion.Service
{
    public class ReputationResponseService : ResponseService
    {
        public ReputationService ReputationService { get; init; }

        public AnnounceIntermediateResultResponse AnnounceIntermediateResult(AnnounceIntermediateResultRequest request, CancellationToken cancellation)
        {
            return (AnnounceIntermediateResultResponse)HandleUnaryRequest(request, r =>
            {
                return ReputationService.AnnounceIntermediateResult((AnnounceIntermediateResultRequest)r, cancellation);
            });
        }
        public AnnounceLocalTrustResponse AnnounceLocalTrust(AnnounceLocalTrustRequest request, CancellationToken cancellation)
        {
            return (AnnounceLocalTrustResponse)HandleUnaryRequest(request, r =>
            {
                return ReputationService.AnnounceLocalTrust((AnnounceLocalTrustRequest)r, cancellation);
            });
        }
    }
}
