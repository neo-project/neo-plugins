using System;
using System.Threading;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.Storage.Services.Object.Get.Writer;
using Neo.FileStorage.Storage.Services.Object.Util;

namespace Neo.FileStorage.Storage.Services.Object.Get
{
    public partial class GetService
    {
        public GetPrm ToGetPrm(GetRequest request, Action<GetResponse> handler, CancellationToken cancellation)
        {
            var key = KeyStorage.GetKey(request.MetaHeader.SessionToken);
            var prm = GetPrm.FromRequest(request);
            prm.Key = key;
            prm.Writer = new GetStream
            {
                Handler = handler
            };
            if (!prm.Local)
                prm.Forwarder = new(key, request, cancellation);
            return prm;
        }

        public HeadPrm ToHeadPrm(HeadRequest request, HeadResponse response, CancellationToken cancellation)
        {
            var key = KeyStorage.GetKey(request.MetaHeader.SessionToken);
            var prm = HeadPrm.FromRequest(request);
            prm.Key = key;
            prm.Writer = new HeadResponseWriter
            {
                Short = request.Body.MainOnly,
                Response = response,
            };
            if (!prm.Local)
                prm.Forwarder = new(key, request, cancellation);
            return prm;
        }

        public RangeHashPrm ToRangeHashPrm(GetRangeHashRequest request)
        {
            var key = KeyStorage.GetKey(request.MetaHeader.SessionToken);
            var prm = RangeHashPrm.FromRequest(request);
            prm.Key = key;
            return prm;
        }

        public RangePrm ToRangePrm(GetRangeRequest request, Action<GetRangeResponse> handler, CancellationToken cancellation)
        {
            var key = KeyStorage.GetKey(request.MetaHeader.SessionToken);
            var prm = RangePrm.FromRequest(request);
            prm.Key = key;
            prm.Writer = new RangeStream
            {
                Handler = handler,
            };
            if (!prm.Local)
                prm.Forwarder = new(key, request, cancellation);
            return prm;
        }
    }
}
