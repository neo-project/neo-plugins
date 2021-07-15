using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.Services.Object.Util;
using Neo.FileStorage.Storage.Services.Object.Get.Writer;

namespace Neo.FileStorage.Storage.Services.Object.Get
{
    public class HeadPrm : GetCommonPrm
    {
        public bool Short;

        public static HeadPrm FromRequest(HeadRequest request)
        {
            var prm = new HeadPrm
            {
                Short = request.Body.MainOnly,
                Address = request.Body.Address,
                Raw = request.Body.Raw,
            };
            prm.WithCommonPrm(CommonPrm.FromRequest(request));
            return prm;
        }
    }
}
