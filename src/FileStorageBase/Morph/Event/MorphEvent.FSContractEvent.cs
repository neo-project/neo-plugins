using Neo.Cryptography.ECC;
using Neo.IO;
using System;
using System.Collections.Generic;

namespace Neo.FileStorage.Morph.Event
{
    partial class MorphEvent
    {
        public class BindEvent : IContractEvent
        {
            public UInt160 UserAccount;
            public ECPoint[] Keys;

            public void ContractEvent() { }

            public static BindEvent ParseBindEvent(VM.Types.Array eventParams)
            {
                var bindEvent = new BindEvent();
                if (eventParams.Count != 2) throw new Exception();
                bindEvent.UserAccount = eventParams[0].GetSpan().AsSerializable<UInt160>();
                List<ECPoint> keys = new List<ECPoint>();
                var bindKeys = ((VM.Types.Array)eventParams[1]).GetEnumerator();
                while (bindKeys.MoveNext())
                {
                    var key = bindKeys.Current.GetSpan().AsSerializable<ECPoint>();
                    keys.Add(key);
                }
                bindEvent.Keys = keys.ToArray();
                return bindEvent;
            }
        }

        public class ChequeEvent : IContractEvent
        {
            public byte[] Id;
            public long Amount;
            public UInt160 UserAccount;
            public UInt160 LockAccount;

            public void ContractEvent() { }

            public static ChequeEvent ParseChequeEvent(VM.Types.Array eventParams)
            {
                var chequeEvent = new ChequeEvent();
                if (eventParams.Count != 4) throw new Exception();
                chequeEvent.Id = eventParams[0].GetSpan().ToArray();
                chequeEvent.UserAccount = eventParams[1].GetSpan().AsSerializable<UInt160>();
                chequeEvent.Amount = (long)eventParams[2].GetInteger();
                chequeEvent.LockAccount = eventParams[3].GetSpan().AsSerializable<UInt160>();
                return chequeEvent;
            }
        }

        public class DepositEvent : IContractEvent
        {
            public byte[] Id;
            public long Amount;
            public UInt160 From;
            public UInt160 To;

            public void ContractEvent() { }

            public static DepositEvent ParseDepositEvent(VM.Types.Array eventParams)
            {
                var depositEvent = new DepositEvent();
                if (eventParams.Count != 4) throw new Exception();
                depositEvent.From = eventParams[0].GetSpan().AsSerializable<UInt160>();
                depositEvent.Amount = (long)eventParams[1].GetInteger();
                depositEvent.To = eventParams[2].GetSpan().AsSerializable<UInt160>();
                depositEvent.Id = eventParams[3].GetSpan().ToArray();
                return depositEvent;
            }
        }

        public class WithdrawEvent : IContractEvent
        {
            public byte[] Id;
            public long Amount;
            public UInt160 UserAccount;

            public void ContractEvent() { }

            public static WithdrawEvent ParseWithdrawEvent(VM.Types.Array eventParams)
            {
                var withdrawEvent = new WithdrawEvent();
                if (eventParams.Count != 3) throw new Exception();
                withdrawEvent.UserAccount = eventParams[0].GetSpan().AsSerializable<UInt160>();
                withdrawEvent.Amount = (long)eventParams[1].GetInteger();
                withdrawEvent.Id = eventParams[2].GetSpan().ToArray();
                return withdrawEvent;
            }
        }

        public class ConfigEvent : IContractEvent
        {
            public byte[] Key;
            public byte[] Value;
            public byte[] Id;

            public void ContractEvent() { }

            public static ConfigEvent ParseConfigEvent(VM.Types.Array eventParams)
            {
                var configEvent = new ConfigEvent();
                if (eventParams.Count != 3) throw new Exception();
                configEvent.Id = eventParams[0].GetSpan().ToArray();
                configEvent.Key = eventParams[1].GetSpan().ToArray();
                configEvent.Value = eventParams[2].GetSpan().ToArray();
                return configEvent;
            }
        }

        public class UpdateInnerRingEvent : IContractEvent
        {
            public ECPoint[] Keys;

            public void ContractEvent() { }

            public static UpdateInnerRingEvent ParseUpdateInnerRingEvent(VM.Types.Array eventParams)
            {
                var updateInnerRingEvent = new UpdateInnerRingEvent();
                if (eventParams.Count != 1) throw new Exception();

                List<ECPoint> keys = new List<ECPoint>();
                var irKeys = ((VM.Types.Array)eventParams[0]).GetEnumerator();
                while (irKeys.MoveNext())
                {
                    var key = irKeys.Current.GetSpan().AsSerializable<ECPoint>();
                    keys.Add(key);
                }
                updateInnerRingEvent.Keys = keys.ToArray();
                return updateInnerRingEvent;
            }
        }
    }
}
