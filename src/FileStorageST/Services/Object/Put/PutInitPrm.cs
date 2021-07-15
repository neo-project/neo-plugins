using System;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.Storage.Services.Object.Util;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Put
{
    public class PutInitPrm : CommonPrm
    {
        public FSObject Header;
        public Action<IFSClient> Relay;

        public static PutInitPrm FromRequest(PutRequest request)
        {
            var body = request?.Body;
            if (body is null) throw new InvalidOperationException($"{nameof(PutInitPrm)} invalid request, body missing");
            if (body.ObjectPartCase != PutRequest.Types.Body.ObjectPartOneofCase.Init)
                throw new InvalidOperationException($"{nameof(PutInitPrm)} invalid init request");
            var init = body.Init;
            if (init is null)
                throw new InvalidOperationException($"{nameof(PutInitPrm)} invalid init request, missing init");
            var obj = new FSObject
            {
                ObjectId = init.ObjectId,
                Signature = init.Signature,
                Header = init.Header,
            };
            var prm = new PutInitPrm
            {
                Header = obj,
            };
            prm.WithCommonPrm(CommonPrm.FromRequest(request));
            return prm;
        }
    }
}
