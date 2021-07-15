
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Session;

namespace Neo.FileStorage.Storage.Services.Object.Util
{
    public static class Helper
    {
        public static SessionToken OriginalSessionToken(RequestMetaHeader meta)
        {
            while (meta.Origin is not null)
                meta = meta.Origin;
            return meta.SessionToken;
        }

        public static BearerToken OriginalBearerToken(RequestMetaHeader meta)
        {
            while (meta.Origin is not null)
                meta = meta.Origin;
            return meta.BearerToken;
        }
    }
}
