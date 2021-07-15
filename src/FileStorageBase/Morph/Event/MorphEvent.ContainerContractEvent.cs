using System;
using Neo.Cryptography.ECC;
using Neo.IO;

namespace Neo.FileStorage.Morph.Event
{
    public partial class MorphEvent
    {
        public class ContainerDeleteEvent : IContractEvent
        {
            public byte[] ContainerID;
            public byte[] Signature;
            public byte[] token;

            public void ContractEvent() { }

            public static ContainerDeleteEvent ParseContainerDeleteEvent(VM.Types.Array eventParams)
            {
                var containerDeleteEvent = new ContainerDeleteEvent();
                if (eventParams.Count != 3) throw new Exception();
                containerDeleteEvent.ContainerID = eventParams[0].GetSpan().ToArray();
                containerDeleteEvent.Signature = eventParams[1].GetSpan().ToArray();
                containerDeleteEvent.token = eventParams[2] is VM.Types.Null ? null : eventParams[2].GetSpan().ToArray();
                return containerDeleteEvent;
            }
        }

        public class ContainerPutEvent : IContractEvent
        {
            public byte[] RawContainer;
            public byte[] Signature;
            public byte[] PublicKey;
            public byte[] token;

            public void ContractEvent() { }

            public static ContainerPutEvent ParseContainerPutEvent(VM.Types.Array eventParams)
            {
                var containerPutEvent = new ContainerPutEvent();
                if (eventParams.Count != 4) throw new Exception();
                containerPutEvent.RawContainer = eventParams[0].GetSpan().ToArray();
                containerPutEvent.Signature = eventParams[1].GetSpan().ToArray();
                containerPutEvent.PublicKey = eventParams[2].GetSpan().ToArray();
                containerPutEvent.token = eventParams[3] is VM.Types.Null ? null : eventParams[3].GetSpan().ToArray();
                return containerPutEvent;
            }
        }

        public class ContainerSetEACLEvent : IContractEvent
        {
            public byte[] Table;
            public byte[] Signature;
            public byte[] PublicKey;
            public byte[] Token;

            public void ContractEvent() { }

            public static ContainerSetEACLEvent ParseContainerSetEACLEvent(VM.Types.Array eventParams)
            {
                var containerSetEACLEvent = new ContainerSetEACLEvent();
                if (eventParams.Count != 4) throw new Exception();
                containerSetEACLEvent.Table = eventParams[0].GetSpan().ToArray();
                containerSetEACLEvent.Signature = eventParams[1].GetSpan().ToArray();
                containerSetEACLEvent.PublicKey = eventParams[2].GetSpan().ToArray();
                containerSetEACLEvent.Token = eventParams[3].GetSpan().ToArray();
                return containerSetEACLEvent;
            }
        }

        public class StartEstimationEvent : IContractEvent
        {
            public ulong Epoch;

            public void ContractEvent() { }

            public static StartEstimationEvent ParseStartEstimationEvent(VM.Types.Array eventParams)
            {
                var startEstimationEvent = new StartEstimationEvent();
                if (eventParams.Count != 1) throw new Exception();
                startEstimationEvent.Epoch = (ulong)eventParams[0].GetInteger();
                return startEstimationEvent;
            }
        }

        public class StopEstimationEvent : IContractEvent
        {
            public ulong Epoch;

            public void ContractEvent() { }

            public static StopEstimationEvent ParseStopEstimationEvent(VM.Types.Array eventParams)
            {
                var stopEstimationEvent = new StopEstimationEvent();
                if (eventParams.Count != 1) throw new Exception();
                stopEstimationEvent.Epoch = (ulong)eventParams[0].GetInteger();
                return stopEstimationEvent;
            }
        }
    }
}
