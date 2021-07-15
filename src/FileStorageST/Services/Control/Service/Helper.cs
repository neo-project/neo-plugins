using System.Security.Cryptography;
using Google.Protobuf;
using Neo.FileStorage.API.Cryptography;

namespace Neo.FileStorage.Storage.Services.Control.Service
{
    public static class Helper
    {
        public static bool VerifyMessage(this ISignedMessage message)
        {
            using var key = message.Signature.Key.ToByteArray().LoadPublicKey();
            return key.VerifyData(message.SignedData.ToByteArray(), message.Signature.Sign.ToByteArray());
        }

        public static void SignMessage(this ECDsa key, ISignedMessage message)
        {
            message.Signature = new()
            {
                Key = ByteString.CopyFrom(key.PublicKey()),
                Sign = ByteString.CopyFrom(key.SignData(message.SignedData.ToByteArray())),
            };
        }
    }
}
