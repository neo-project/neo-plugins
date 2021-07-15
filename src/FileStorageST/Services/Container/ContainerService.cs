using System.Threading;
using Neo.FileStorage.API.Container;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Storage.Services.Container.Announcement;
using static Neo.FileStorage.Storage.Services.Object.Util.Helper;
using FSContainer = Neo.FileStorage.API.Container.Container;

namespace Neo.FileStorage.Storage.Services.Container
{
    public class ContainerService
    {
        public MorphInvoker MorphInvoker { get; init; }
        public UsedSpaceService UsedSpaceService { get; init; }

        public AnnounceUsedSpaceResponse AnnounceUsedSpace(AnnounceUsedSpaceRequest request, CancellationToken context)
        {
            return UsedSpaceService.AnnounceUsedSpace(request, context);
        }

        public DeleteResponse Delete(DeleteRequest request)
        {
            byte[] sig = request.Body.Signature.Sign.ToByteArray();
            MorphInvoker.DeleteContainer(request.Body.ContainerId, sig, OriginalSessionToken(request.MetaHeader));
            var resp = new DeleteResponse
            {
                Body = new DeleteResponse.Types.Body { }
            };
            return resp;
        }

        public GetResponse Get(GetRequest request)
        {
            var cnr = MorphInvoker.GetContainer(request.Body.ContainerId);
            var resp = new GetResponse
            {
                Body = new GetResponse.Types.Body
                {
                    Container = cnr.Container,
                    Signature = cnr.Signature,
                    SessionToken = cnr.SessionToken
                }
            };
            return resp;
        }

        public GetExtendedACLResponse GetExtendedACL(GetExtendedACLRequest request)
        {
            var result = MorphInvoker.GetEACL(request.Body.ContainerId);
            var resp = new GetExtendedACLResponse
            {
                Body = new GetExtendedACLResponse.Types.Body
                {
                    Eacl = result.Table,
                    Signature = result.Signature,
                }
            };
            return resp;
        }

        public ListResponse List(ListRequest request)
        {
            var containers = MorphInvoker.ListContainers(request.Body.OwnerId);
            var resp = new ListResponse { Body = new ListResponse.Types.Body { } };
            resp.Body.ContainerIds.AddRange(containers);
            return resp;
        }

        public PutResponse Put(PutRequest request)
        {
            FSContainer container = request.Body.Container;
            MorphInvoker.PutContainer(container, request.Body.Signature, OriginalSessionToken(request.MetaHeader));
            var resp = new PutResponse
            {
                Body = new PutResponse.Types.Body
                {
                    ContainerId = container.CalCulateAndGetId,
                }
            };
            return resp;
        }

        public SetExtendedACLResponse SetExtendedACL(SetExtendedACLRequest request)
        {
            MorphInvoker.SetEACL(request.Body.Eacl, request.Body.Signature, OriginalSessionToken(request.MetaHeader));
            var resp = new SetExtendedACLResponse
            {
                Body = new SetExtendedACLResponse.Types.Body { }
            };
            return resp;
        }
    }
}
