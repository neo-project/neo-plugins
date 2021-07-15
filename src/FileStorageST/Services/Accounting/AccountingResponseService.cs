using Neo.FileStorage.API.Accounting;

namespace Neo.FileStorage.Storage.Services.Accounting
{
    public class AccountingResponseService : ResponseService
    {
        public AccountingService AccountingService { get; init; }

        public BalanceResponse Balance(BalanceRequest request)
        {
            return (BalanceResponse)HandleUnaryRequest(request, r =>
            {
                return AccountingService.Balance((BalanceRequest)r);
            });
        }
    }
}
