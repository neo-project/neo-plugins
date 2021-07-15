using Grpc.Core;
using Neo.FileStorage.API.Accounting;
using System.Threading.Tasks;
using APIAccountingService = Neo.FileStorage.API.Accounting.AccountingService;

namespace Neo.FileStorage.Storage.Services.Accounting
{
    public class AccountingServiceImpl : APIAccountingService.AccountingServiceBase
    {
        public AccountingSignService SignService { get; init; }

        public override Task<BalanceResponse> Balance(BalanceRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.Balance(request);
            }, context.CancellationToken);
        }
    }
}
