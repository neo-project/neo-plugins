using Grpc.Core;
using Neo.FileStorage.API.Session;

namespace Neo.FileStorage.Services.Session
{
    public interface IServiceExecutor
    {
        CreateResponse.Types.Body Create(ServerCallContext ctx, CreateRequest.Types.Body body);
    }

    public class ExecutorSvc
    {
        private IServiceExecutor exec;

        public ExecutorSvc(IServiceExecutor se)
        {
            this.exec = se;
        }

        public CreateResponse Create(ServerCallContext ctx, CreateRequest req)
        {
            var respBody = this.exec.Create(ctx, req.Body);
            var resp = new CreateResponse() { Body = respBody };
            return resp;
        }
    }
}