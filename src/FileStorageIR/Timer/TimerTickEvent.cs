using Neo.FileStorage.Morph.Event;

namespace Neo.FileStorage.InnerRing.Timer
{
    public class TimerTickEvent
    {
        public class NewEpochTickEvent : IContractEvent
        {
            public void ContractEvent() { }
        }

        public class NewAlphabetEmitTickEvent : IContractEvent
        {
            public void ContractEvent() { }
        }

        public class NetmapCleanupTickEvent : IContractEvent
        {
            public ulong Epoch;

            public void ContractEvent() { }
        }
    }
}
