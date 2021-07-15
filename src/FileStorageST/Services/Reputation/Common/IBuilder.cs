using System.Collections.Generic;
using Neo.FileStorage.API.Netmap;

namespace Neo.FileStorage.Storage.Services.Reputaion.Common
{
    public interface IBuilder
    {
        List<NodeInfo> NextStage(ulong epoch, Trust trust, List<NodeInfo> passed);
    }
}
