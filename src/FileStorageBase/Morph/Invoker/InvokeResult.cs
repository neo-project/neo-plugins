using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;

namespace Neo.FileStorage.Morph.Invoker
{
    public class InvokeResult
    {
        public VMState State;
        public long GasConsumed;
        public byte[] Script;
        public StackItem[] ResultStack;

        public override string ToString()
        {
            return string.Format("InvokeResult:VMState:{0}:GasConsumed:{1}:Script:{2}:ResultStack:{3}", State, GasConsumed, Script.ToHexString(), ResultStack.Length);
        }
    }
}
