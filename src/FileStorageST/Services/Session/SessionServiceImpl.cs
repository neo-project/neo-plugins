using Grpc.Core;
using Neo.FileStorage.API.Session;
using System.Threading.Tasks;
using APISessionService = Neo.FileStorage.API.Session.SessionService;

namespace Neo.FileStorage.Storage.Services.Session
{
    public class SessionServiceImpl : APISessionService.SessionServiceBase
    {
        public SessionSignService SignService { get; init; }

        public override Task<CreateResponse> Create(CreateRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.Create(request);
            }, context.CancellationToken);
        }
    }
}
