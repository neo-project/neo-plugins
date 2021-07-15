using System;
using System.Collections.Generic;
using Neo.FileStorage.API.Audit;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.FileStorage.Morph.Invoker
{
    public partial class MorphInvoker
    {
        private const string PutResultMethod = "put";
        private const string GetResultMethod = "get";
        private const string ListResultsMethod = "list";
        private const string ListByEpochResultsMethod = "listByEpoch";
        private const string ListByCIDResultsMethod = "listByCID";
        private const string ListByNodeResultsMethod = "listByNode";


        public bool PutAuditResult(byte[] rawResult)
        {
            return Invoke(out _, AuditContractHash, PutResultMethod, SideChainFee, rawResult);
        }

        public DataAuditResult GetAuditResult(byte[] id)
        {
            InvokeResult result = TestInvoke(AuditContractHash, GetResultMethod, id);
            if (result.State != VM.VMState.HALT) throw new Exception(string.Format("could not perform test invocation ({0})", GetResultMethod));
            return DataAuditResult.Parser.ParseFrom(result.ResultStack[0].GetSpan().ToArray());
        }

        public List<byte[]> ListAuditResults()
        {
            InvokeResult result = TestInvoke(AuditContractHash, ListResultsMethod);
            if (result.State != VM.VMState.HALT) throw new Exception(string.Format("could not perform test invocation ({0})", ListResultsMethod));
            return ParseAuditResults(result.ResultStack[0]);
        }

        public List<byte[]> ListAuditResultsByEpoch(long epoch)
        {
            InvokeResult result = TestInvoke(AuditContractHash, ListByEpochResultsMethod, epoch);
            if (result.State != VM.VMState.HALT) throw new Exception(string.Format("could not perform test invocation ({0})", ListByEpochResultsMethod));
            return ParseAuditResults(result.ResultStack[0]);
        }

        public List<byte[]> ListAuditResultsByCID(long epoch, byte[] cid)
        {
            InvokeResult result = TestInvoke(AuditContractHash, ListByCIDResultsMethod, epoch, cid);
            if (result.State != VM.VMState.HALT) throw new Exception(string.Format("could not perform test invocation ({0})", ListByEpochResultsMethod));
            return ParseAuditResults(result.ResultStack[0]);
        }

        public List<byte[]> ListAuditResultsByNode(long epoch, byte[] cid, byte[] nodeKey)
        {
            InvokeResult result = TestInvoke(AuditContractHash, ListByNodeResultsMethod, epoch, cid, nodeKey);
            if (result.State != VM.VMState.HALT) throw new Exception(string.Format("could not perform test invocation ({0})", ListByEpochResultsMethod));
            return ParseAuditResults(result.ResultStack[0]);
        }

        public List<byte[]> ParseAuditResults(StackItem result)
        {
            if (result is Null) return new List<byte[]>();
            Array array = (Array)result;
            IEnumerator<StackItem> enumerator = array.GetEnumerator();
            List<byte[]> resultArray = new();
            while (enumerator.MoveNext())
            {
                resultArray.Add(enumerator.Current.GetSpan().ToArray());
            }
            return resultArray;
        }
    }
}
