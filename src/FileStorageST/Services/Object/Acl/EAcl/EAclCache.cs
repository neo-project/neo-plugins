
using System;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Cache;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Storage.Services.Object.Acl.EAcl;

namespace Neo.FileStorage.Storage.Services.Object.Acl
{
    public class EAclCache : IEAclSource
    {
        public const int DefaultCacheSize = 100;
        public const int DefaultTTLMilliseconds = 30000;

        private readonly TTLNetworkCache<ContainerID, EACLTable> cache;

        public EAclCache(MorphInvoker invoker, int size = DefaultCacheSize, int ttl = DefaultTTLMilliseconds)
        {
            cache = new(size, TimeSpan.FromMilliseconds(ttl), cid =>
             {
                 var result = invoker.GetEACL(cid);
                 if (!result.Signature.VerifyMessagePart(result.Table))
                     throw new InvalidOperationException("incorrect signature of eacl from morph client");
                 return result.Table;
             });
        }

        public EACLTable GetEACL(ContainerID cid)
        {
            return cache.Get(cid);
        }

    }
}
