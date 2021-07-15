using Neo.FileStorage.Morph.Event;
using Neo.SmartContract.Native;
using System;

namespace Neo.FileStorage.InnerRing.Events
{
    public partial class MorphEvent
    {
        public class SyncEvent : IContractEvent
        {
            public ulong epoch;
            public void ContractEvent() { }
        }

        public class DesignateEvent : IContractEvent
        {
            public byte role;
            public void ContractEvent() { }

            public static DesignateEvent ParseDesignateEvent(VM.Types.Array eventParams)
            {
                var designateEvent = new DesignateEvent();
                if (eventParams.Count != 2) throw new Exception();
                designateEvent.role = (byte)eventParams[0].GetInteger();
                return designateEvent;
            }
        }
    }
}
