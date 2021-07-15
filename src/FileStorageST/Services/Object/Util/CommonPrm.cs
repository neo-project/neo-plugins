using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Session;
using Neo.FileStorage.API.Client;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace Neo.FileStorage.Storage.Services.Object.Util
{
    public class CommonPrm
    {
        public bool Local;
        public ulong NetmapEpoch;
        public ulong NetmapLookupDepth;
        public SessionToken SessionToken;
        public BearerToken BearerToken;
        public ECDsa Key;
        public CallOptions CallOptions;
        public int SuccessAfter = 0;
        public bool TrackCopies = true;

        public static CommonPrm FromRequest(IRequest request)
        {
            var meta = request.MetaHeader;
            var options = new CallOptions();
            var xheaders = new List<XHeader>();
            var prm = new CommonPrm
            {
                Local = meta.Ttl <= 1,
            };
            if (meta.SessionToken is not null)
            {
                prm.SessionToken = meta.SessionToken;
                options.Session = meta.SessionToken;
            }
            if (meta.BearerToken is not null)
            {
                prm.BearerToken = meta.BearerToken;
                options.Bearer = meta.BearerToken;
            }
            foreach (var xheader in meta.XHeaders)
            {
                switch (xheader.Key)
                {
                    case XHeader.XHeaderNetmapEpoch:
                        prm.NetmapEpoch = uint.Parse(xheader.Value);
                        break;
                    case XHeader.XHeaderNetmapLookupDepth:
                        prm.NetmapLookupDepth = uint.Parse(xheader.Value);
                        break;
                    default:
                        xheaders.Add(xheader);
                        break;
                }
            }
            options.XHeaders = xheaders;
            prm.CallOptions = options;
            return prm;
        }

        public CommonPrm WithCommonPrm(CommonPrm other)
        {
            Local = other.Local;
            NetmapEpoch = other.NetmapEpoch;
            NetmapLookupDepth = other.NetmapLookupDepth;
            SessionToken = other.SessionToken;
            BearerToken = other.BearerToken;
            Key = other.Key;
            CallOptions = other.CallOptions;
            SuccessAfter = other.SuccessAfter;
            TrackCopies = other.TrackCopies;
            return this;
        }
    }
}
