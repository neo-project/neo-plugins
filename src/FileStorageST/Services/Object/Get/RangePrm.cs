using Neo.FileStorage.API.Object;
using V2Range = Neo.FileStorage.API.Object.Range;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.Services.Object.Util;
using Neo.FileStorage.Storage.Services.Object.Get.Writer;

namespace Neo.FileStorage.Storage.Services.Object.Get
{
    public class RangePrm : GetCommonPrm
    {
        public V2Range Range;

        public static RangePrm FromRequest(GetRangeRequest request)
        {
            var prm = new RangePrm
            {
                Address = request.Body.Address,
                Range = request.Body.Range,
            };
            prm.WithCommonPrm(CommonPrm.FromRequest(request));
            return prm;
        }
    }
}
