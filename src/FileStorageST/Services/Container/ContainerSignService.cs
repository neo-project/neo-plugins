using System.Threading;
using Neo.FileStorage.API.Container;

namespace Neo.FileStorage.Storage.Services.Container
{
    public class ContainerSignService : SignService
    {
        public ContainerResponseService ResponseService { get; init; }

        public AnnounceUsedSpaceResponse AnnounceUsedSpace(AnnounceUsedSpaceRequest request, CancellationToken context)
        {
            return (AnnounceUsedSpaceResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.AnnounceUsedSpace((AnnounceUsedSpaceRequest)r, context);
            });
        }

        public DeleteResponse Delete(DeleteRequest request)
        {
            return (DeleteResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.Delete((DeleteRequest)r);
            });
        }

        public GetResponse Get(GetRequest request)
        {
            return (GetResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.Get((GetRequest)r);
            });
        }

        public GetExtendedACLResponse GetExtendedACL(GetExtendedACLRequest request)
        {
            return (GetExtendedACLResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.GetExtendedACL((GetExtendedACLRequest)r);
            });
        }

        public ListResponse List(ListRequest request)
        {
            return (ListResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.List((ListRequest)r);
            });
        }

        public PutResponse Put(PutRequest request)
        {
            return (PutResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.Put((PutRequest)r);
            });
        }

        public SetExtendedACLResponse SetExtendedACL(SetExtendedACLRequest request)
        {
            return (SetExtendedACLResponse)HandleUnaryRequest(request, r =>
            {
                return ResponseService.SetExtendedACL((SetExtendedACLRequest)r);
            });
        }
    }
}
