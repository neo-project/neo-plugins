using System.Security.Cryptography;

namespace Neo.FileStorage.Storage.Services.Session.Storage
{
    public class PrivateToken
    {
        private ECDsa sessionKey;
        private ulong exp;

        public ECDsa SessionKey
        {
            get => sessionKey;
        }

        public PrivateToken(ECDsa sk, ulong expiration)
        {
            sessionKey = sk;
            exp = expiration;
        }
    }
}
