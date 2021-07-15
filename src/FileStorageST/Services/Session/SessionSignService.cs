using Neo.FileStorage.API.Session;

namespace Neo.FileStorage.Storage.Services.Session
{
    public class SessionSignService : SignService
    {
        public SessionResponseService ResponseService { get; init; }

        public CreateResponse Create(CreateRequest request)
        {
            return (CreateResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.Create((CreateRequest)r);
            });
        }
    }
}
