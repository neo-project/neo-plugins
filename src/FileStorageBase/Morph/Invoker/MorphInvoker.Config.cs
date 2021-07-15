using System;

namespace Neo.FileStorage.Morph.Invoker
{
    public partial class MorphInvoker
    {
        public readonly byte[] MaxObjectSizeConfig = Utility.StrictUTF8.GetBytes("MaxObjectSize");
        public readonly byte[] BasicIncomeRateConfig = Utility.StrictUTF8.GetBytes("BasicIncomeRate");
        public readonly byte[] AuditFeeConfig = Utility.StrictUTF8.GetBytes("AuditFee");
        public readonly byte[] EpochDurationConfig = Utility.StrictUTF8.GetBytes("EpochDuration");
        public readonly byte[] ContainerFeeConfig = Utility.StrictUTF8.GetBytes("ContainerFee");
        public readonly byte[] EigenTrustIterationsConfig = Utility.StrictUTF8.GetBytes("EigenTrustIterations");
        public readonly byte[] EigenTrustAlphaConfig = Utility.StrictUTF8.GetBytes("EigenTrustAlpha");
        public readonly byte[] InnerRingCandidateFeeConfig = Utility.StrictUTF8.GetBytes("InnerRingCandidateFee");
        public readonly byte[] WithdrawFeeConfig = Utility.StrictUTF8.GetBytes("WithdrawFee");

        private const string ConfigMethod = "config";

        private ulong ReadUInt64Config(byte[] key)
        {
            InvokeResult result = TestInvoke(NetMapContractHash, ConfigMethod, key);
            if (result.State != VM.VMState.HALT) throw new Exception($"could not invoke method {nameof(ReadUInt64Config)}");
            return (ulong)result.ResultStack[0].GetInteger();
        }

        private string ReadStringConfig(byte[] key)
        {
            InvokeResult result = TestInvoke(NetMapContractHash, ConfigMethod, key);
            if (result.State != VM.VMState.HALT) throw new Exception($"could not invoke method {nameof(ReadStringConfig)}");
            return result.ResultStack[0].GetString();
        }

        public ulong MaxObjectSize()
        {
            return ReadUInt64Config(MaxObjectSizeConfig);
        }

        public ulong BasicIncomeRate()
        {
            return ReadUInt64Config(BasicIncomeRateConfig);
        }

        public ulong AuditFee()
        {
            return ReadUInt64Config(AuditFeeConfig);
        }

        public ulong EpochDuration()
        {
            return ReadUInt64Config(EpochDurationConfig);
        }

        public ulong ContainerFee()
        {
            return ReadUInt64Config(ContainerFeeConfig);
        }

        public ulong EigenTrustIterations()
        {
            return ReadUInt64Config(EigenTrustIterationsConfig);
        }

        public double EigenTrustAlpha()
        {
            return double.Parse(ReadStringConfig(EigenTrustAlphaConfig));
        }

        public ulong InnerRingCandidateFee()
        {
            return ReadUInt64Config(InnerRingCandidateFeeConfig);
        }

        public ulong WithdrawFee()
        {
            return ReadUInt64Config(WithdrawFeeConfig);
        }
    }
}
