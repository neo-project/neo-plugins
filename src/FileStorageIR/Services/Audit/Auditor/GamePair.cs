using System.Collections.Generic;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.InnerRing.Services.Audit.Auditor
{
    public class GamePair
    {
        public Node N1;
        public Node N2;
        public ObjectID Id;
        public List<Range> Range1;
        public List<Range> Range2;
        public List<byte[]> Hashes1;
        public List<byte[]> Hashes2;
    }
}
