using Neo.FileStorage.Storage.Services.Object.Util;
using FSAddress = Neo.FileStorage.API.Refs.Address;

namespace Neo.FileStorage.Storage.Services.Object.Head
{
    public class RemoteHeadPrm : CommonPrm
    {
        public FSAddress Address;
        public bool Short;
        public bool Raw;
        public Network.Address Node;
    }
}
