using System;

namespace Neo.FileStorage.Storage.Services.Reputaion.Local.Storage
{
    public class UpdatePrm
    {
        public bool Sat;
        public ulong Epoch;
        public PeerID PeerId;

        public UpdatePrm(PeerID id)
        {
            if (id is null) throw new ArgumentNullException(nameof(id));
            PeerId = id;
        }
    }
}
