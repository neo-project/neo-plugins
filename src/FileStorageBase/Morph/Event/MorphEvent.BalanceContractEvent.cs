using Neo.IO;
using System;

namespace Neo.FileStorage.Morph.Event
{
    public partial class MorphEvent
    {
        public class LockEvent : IContractEvent
        {
            public byte[] Id;
            public UInt160 UserAccount;
            public UInt160 LockAccount;
            public long Amount;
            public long Util;

            public void ContractEvent() { }

            public static LockEvent ParseLockEvent(VM.Types.Array eventParams)
            {
                var lockEvent = new LockEvent();
                if (eventParams.Count != 5) throw new Exception();
                lockEvent.Id = eventParams[0].GetSpan().ToArray();
                lockEvent.UserAccount = eventParams[1].GetSpan().AsSerializable<UInt160>();
                lockEvent.LockAccount = eventParams[2].GetSpan().AsSerializable<UInt160>();
                lockEvent.Amount = (long)eventParams[3].GetInteger();
                lockEvent.Util = (long)eventParams[4].GetInteger();
                return lockEvent;
            }
        }
    }
}
