using System.Threading.Tasks;
using Grpc.Core;
using Neo.FileStorage.API.Container;
using APIContainerService = Neo.FileStorage.API.Container.ContainerService;

namespace Neo.FileStorage.Storage.Services.Container
{
    public class ContainerServiceImpl : APIContainerService.ContainerServiceBase
    {
        public ContainerSignService SignService { get; init; }

        public override Task<AnnounceUsedSpaceResponse> AnnounceUsedSpace(AnnounceUsedSpaceRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.AnnounceUsedSpace(request, context.CancellationToken);
            }, context.CancellationToken);
        }

        public override Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.Delete(request);
            }, context.CancellationToken);
        }

        public override Task<GetResponse> Get(GetRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.Get(request);
            }, context.CancellationToken);
        }

        public override Task<GetExtendedACLResponse> GetExtendedACL(GetExtendedACLRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.GetExtendedACL(request);
            }, context.CancellationToken);
        }

        public override Task<ListResponse> List(ListRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.List(request);
            }, context.CancellationToken);
        }

        public override Task<PutResponse> Put(PutRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.Put(request);
            }, context.CancellationToken);
        }

        public override Task<SetExtendedACLResponse> SetExtendedACL(SetExtendedACLRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                return SignService.SetExtendedACL(request);
            }, context.CancellationToken);
        }
    }
}
