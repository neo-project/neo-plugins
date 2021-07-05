namespace Neo.Consensus
{
    public enum ConsensusMessageType : byte
    {
        ChangeView = 0x00,

        TXListRequest = 0x10,
        TXListMessage = 0x11,

        PrepareRequest = 0x20,
        PrepareResponse = 0x21,

        Commit = 0x30,

        RecoveryRequest = 0x40,
        RecoveryMessage = 0x41,
    }
}
