using Neo.FileStorage.Morph.Event;

namespace Neo.FileStorage.InnerRing.Events
{
    public partial class MorphEvent
    {
        public class AuditStartEvent : IContractEvent
        {
            public ulong epoch;
            public void ContractEvent() { }
        }

        public class BasicIncomeCollectEvent : AuditStartEvent { }
        public class BasicIncomeDistributeEvent : AuditStartEvent { }
    }
}
