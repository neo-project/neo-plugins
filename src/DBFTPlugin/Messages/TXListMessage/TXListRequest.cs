
using System.IO;


namespace Neo.Consensus.Messages
{
    public class TXListRequest : ConsensusMessage
    {

        public override int Size => base.Size;

        public TXListRequest() : base(ConsensusMessageType.TXListRequest) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
        }
    }
}
