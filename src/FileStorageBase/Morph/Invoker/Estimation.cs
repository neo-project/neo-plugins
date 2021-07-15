using System.Collections.Generic;
using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.Morph.Invoker
{
    public class Estimation
    {
        public ulong Size;
        public byte[] Reporter;
    }

    public class Estimations
    {
        public ContainerID ContainerID;
        public List<Estimation> AllEstimation;
    }

}
