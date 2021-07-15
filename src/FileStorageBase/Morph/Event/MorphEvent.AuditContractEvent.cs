namespace Neo.FileStorage.Morph.Event
{
    public partial class MorphEvent
    {
        public class StartEvent : IContractEvent
        {
            public ulong epoch;
            public void ContractEvent() { }
        }
    }
}
