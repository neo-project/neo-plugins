using Neo.FileStorage.API.Accounting;

namespace Neo.FileStorage.Storage.Services.Accounting
{
    public class AccountingSignService : SignService
    {
        public AccountingResponseService ResponseService { get; init; }

        public BalanceResponse Balance(BalanceRequest request)
        {
            return (BalanceResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.Balance((BalanceRequest)r);
            });
        }
    }
}
