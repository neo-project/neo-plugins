using System;

namespace Neo.FileStorage.Morph.Invoker
{
    public partial class MorphInvoker
    {
        private const string BalanceOfMethod = "balanceOf";
        private const string DecimalsMethod = "decimals";
        private const string TransferXMethod = "transferX";
        private const string LockMethod = "lock";
        private const string MintMethod = "mint";
        private const string BurnMethod = "burn";

        public long BalanceOf(byte[] holder)
        {
            InvokeResult result = TestInvoke(BalanceContractHash, BalanceOfMethod, holder);
            if (result.State != VM.VMState.HALT) throw new Exception(string.Format("could not perform test invocation ({0})", BalanceOfMethod));
            return (long)result.ResultStack[0].GetInteger();
        }

        public uint BalanceDecimals()
        {
            InvokeResult result = TestInvoke(BalanceContractHash, DecimalsMethod);
            if (result.State != VM.VMState.HALT) throw new Exception(string.Format("could not perform test invocation ({0})", DecimalsMethod));
            return (uint)(result.ResultStack[0].GetInteger());
        }

        public bool TransferX(byte[] from, byte[] to, long amount, byte[] details)
        {
            return Invoke(out _, BalanceContractHash, TransferXMethod, SideChainFee, from, to, amount, details);
        }

        public bool Mint(byte[] scriptHash, long amount, byte[] comment)
        {
            return Invoke(out _, BalanceContractHash, MintMethod, SideChainFee, scriptHash, amount, comment);
        }

        public bool Burn(byte[] scriptHash, long amount, byte[] comment)
        {
            return Invoke(out _, BalanceContractHash, BurnMethod, SideChainFee, scriptHash, amount, comment);
        }

        public bool LockAsset(byte[] ID, UInt160 userAccount, UInt160 lockAccount, long amount, ulong until)
        {
            return Invoke(out _, BalanceContractHash, LockMethod, SideChainFee, ID, userAccount, lockAccount, amount, (int)until);
        }

        public uint BalancePrecision()
        {
            return BalanceDecimals();
        }
    }
}
