using Google.Protobuf;
using Neo.Cryptography;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.Session;
using Neo.IO;
using Neo.Wallets;
using System;
using System.Collections.Concurrent;

namespace Neo.FileStorage.Storage.Services.Session.Storage
{
    public class TokenStore
    {
        private readonly ConcurrentDictionary<string, PrivateToken> tokens = new();

        public PrivateToken Get(OwnerID owner, byte[] token)
        {
            return tokens[StoreKey(owner, token)];
        }

        public CreateResponse.Types.Body Create(CreateRequest request)
        {
            var gb = Guid.NewGuid().ToByteArray();
            var key = StoreKey(request.Body.OwnerId, gb);
            var sk = new byte[64];
            var random = new Random();
            random.NextBytes(sk);
            tokens[key] = new PrivateToken(sk.LoadPrivateKey(), request.Body.Expiration);
            var keyPair = new KeyPair(sk);
            return new CreateResponse.Types.Body()
            {
                Id = ByteString.CopyFrom(gb),
                SessionKey = ByteString.CopyFrom(keyPair.PublicKey.EncodePoint(true))
            };
        }

        private string StoreKey(OwnerID owner, byte[] token)
        {
            return Convert.ToBase64String(owner.Value.ToByteArray()) + Convert.ToBase64String(token);
        }
    }
}
