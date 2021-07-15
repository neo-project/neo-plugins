using System;
using System.Collections.Generic;
using Neo.FileStorage.API.Reputation;
using Neo.VM.Types;

namespace Neo.FileStorage.Morph.Invoker
{
    public partial class MorphInvoker
    {
        private const string ReputationPutMethod = "put";
        private const string ReputationGetMethod = "get";
        private const string ReputationGetByIDMethod = "getByID";
        private const string ReputationListByEpochMethod = "listByEpoch";

        public bool PutReputation(ulong epoch, byte[] peerID, byte[] value)
        {
            return Invoke(out _, ReputationContractHash, ReputationPutMethod, SideChainFee, epoch, peerID, value);
        }

        public List<GlobalTrust> GetReputation(ulong epoch, byte[] peerID)
        {
            InvokeResult result = TestInvoke(ReputationContractHash, ReputationGetMethod, 0, epoch, peerID);
            if (result.State != VM.VMState.HALT) throw new Exception($"could not perform test invocation ({ReputationGetMethod})");
            if (result.ResultStack.Length != 1) throw new Exception();
            VM.Types.Array items = (VM.Types.Array)result.ResultStack[0];
            IEnumerator<StackItem> itemsEnumerator = items.GetEnumerator();
            List<GlobalTrust> lists = new();
            while (itemsEnumerator.MoveNext())
            {
                lists.Add(GlobalTrust.Parser.ParseFrom(itemsEnumerator.Current.GetSpan().ToArray()));
            }
            return lists;
        }

        public List<GlobalTrust> GetReputationByID(byte[] id)
        {
            InvokeResult result = TestInvoke(ReputationContractHash, ReputationGetByIDMethod, id);
            if (result.State != VM.VMState.HALT) throw new Exception(string.Format("could not perform test invocation ({0})", ReputationGetByIDMethod));
            if (result.ResultStack.Length != 1) throw new Exception();
            VM.Types.Array items = (VM.Types.Array)result.ResultStack[0];
            IEnumerator<StackItem> itemsEnumerator = items.GetEnumerator();
            List<GlobalTrust> lists = new();
            while (itemsEnumerator.MoveNext())
            {
                lists.Add(GlobalTrust.Parser.ParseFrom(itemsEnumerator.Current.GetSpan().ToArray()));
            }
            return lists;
        }

        public List<byte[]> ListReputationByEpoch(long epoch)
        {
            InvokeResult result = TestInvoke(ReputationContractHash, ReputationListByEpochMethod, epoch);
            if (result.State != VM.VMState.HALT) throw new Exception(string.Format("could not perform test invocation ({0})", ReputationListByEpochMethod));
            if (result.ResultStack.Length != 1) throw new Exception();
            VM.Types.Array items = (VM.Types.Array)result.ResultStack[0];
            IEnumerator<StackItem> itemsEnumerator = items.GetEnumerator();
            List<byte[]> ids = new();
            while (itemsEnumerator.MoveNext())
            {
                ids.Add(itemsEnumerator.Current.GetSpan().ToArray());
            }
            return ids;
        }
    }
}
