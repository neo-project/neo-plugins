using System.Security.Cryptography;
using Neo.FileStorage.Morph.Invoker;

namespace Neo.FileStorage.Storage.Services.Reputaion.Intermediate
{
    public class FinalWriterProvider
    {
        public ECDsa PrivateKey { get; init; }
        public byte[] PublicKey { get; init; }
        public MorphInvoker MorphInvoker { get; init; }

        public FinalWriter InitIntermediateWriter()
        {
            return new()
            {
                PrivateKey = PrivateKey,
                PublicKey = PublicKey,
                MorphInvoker = MorphInvoker,
            };
        }
    }
}
