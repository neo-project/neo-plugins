using Neo.FileStorage.API.Session;

namespace Neo.FileStorage.Storage.Services.Session
{
    public class SessionResponseService : ResponseService
    {
        public SessionService SessionService { get; init; }

        public CreateResponse Create(CreateRequest request)
        {
            return (CreateResponse)HandleUnaryRequest(request, r =>
            {
                return SessionService.Create((CreateRequest)r);
            });
        }
    }
}
