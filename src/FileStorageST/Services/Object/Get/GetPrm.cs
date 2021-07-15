using Neo.FileStorage.API.Object;
using Neo.FileStorage.Storage.Services.Object.Util;

namespace Neo.FileStorage.Storage.Services.Object.Get
{
    public class GetPrm : GetCommonPrm
    {
        public static GetPrm FromRequest(GetRequest request)
        {
            var prm = new GetPrm
            {
                Address = request.Body.Address,
                Raw = request.Body.Raw,
            };
            prm.WithCommonPrm(CommonPrm.FromRequest(request));
            return prm;
        }
    }
}
