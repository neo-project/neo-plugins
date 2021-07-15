
using Google.Protobuf;
using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.Storage.Services.Control.Service
{
    public interface ISignedMessage
    {
        IMessage SignedData { get; }
        Signature Signature { get; set; }
    }
}
