using Neo.FileStorage.API.Session;
using Neo.FileStorage.Storage.Services.Session.Storage;

namespace Neo.FileStorage.Storage.Services.Session
{
    public class SessionService
    {
        public TokenStore TokenStore { get; init; }

        public CreateResponse Create(CreateRequest request)
        {
            return new()
            {
                Body = TokenStore.Create(request),
            };
        }
    }
}
