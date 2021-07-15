using System.Threading;
using Neo.FileStorage.API.Reputation;

namespace Neo.FileStorage.Storage.Services.Reputaion.Service
{
    public class ReputationSignService : SignService
    {
        public ReputationResponseService ResponseService { get; init; }

        public AnnounceIntermediateResultResponse AnnounceIntermediateResult(AnnounceIntermediateResultRequest request, CancellationToken cancellation)
        {
            return (AnnounceIntermediateResultResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.AnnounceIntermediateResult((AnnounceIntermediateResultRequest)r, cancellation);
            });
        }
        public AnnounceLocalTrustResponse AnnounceLocalTrust(AnnounceLocalTrustRequest request, CancellationToken cancellation)
        {
            return (AnnounceLocalTrustResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.AnnounceLocalTrust((AnnounceLocalTrustRequest)r, cancellation);
            });
        }
    }
}
