using System;
using Neo.IO;
using System.IO;
using System.Linq;

namespace Neo.Consensus.Messages
{
    public class TXListMessage : ConsensusMessage
    {
        public UInt256[] TransactionHashes;

        public override int Size => base.Size
            + TransactionHashes.GetVarSize();   //TransactionHashes;

        public TXListMessage() : base(ConsensusMessageType.TXListMessage) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            TransactionHashes = reader.ReadSerializableArray<UInt256>(ushort.MaxValue);
            if (TransactionHashes.Distinct().Count() != TransactionHashes.Length)
                throw new FormatException();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(TransactionHashes);
        }
    }
}
