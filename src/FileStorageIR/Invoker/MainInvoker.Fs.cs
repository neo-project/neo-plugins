using System;
using System.Collections.Generic;
using Neo.Cryptography.ECC;
using Neo.FileStorage.Morph.Invoker;
using Neo.SmartContract;

namespace Neo.FileStorage.InnerRing.Invoker
{
    public partial class MainInvoker
    {
        private const string ChequeMethod = "cheque";
        private const string AlphabetUpdateMethod = "alphabetUpdate";

        public bool CashOutCheque(byte[] Id, long amount, UInt160 userAccount, UInt160 lockAccount)
        {
            return Invoke(out _, FsContractHash, ChequeMethod, MainChainFee, Id, userAccount, amount, lockAccount);
        }

        public bool AlphabetUpdate(byte[] Id, ECPoint[] publicKeys)
        {
            var array = new ContractParameter(ContractParameterType.Array);
            var list = new List<ContractParameter>();
            foreach (var publicKey in publicKeys)
            {
                list.Add(new ContractParameter(ContractParameterType.PublicKey) { Value = publicKey });
            }
            array.Value = list;
            return Invoke(out _, FsContractHash, AlphabetUpdateMethod, MainChainFee, Id, array);
        }
    }
}
