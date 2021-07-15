using System;
using System.Collections.Generic;
using Neo.Cryptography.ECC;
using Neo.FileStorage.Morph.Invoker;
using Neo.SmartContract;

namespace Neo.FileStorage.Morph.Invoker
{
    public partial class MorphInvoker
    {
        private const string EmitMethod = "emit";
        private const string VoteMethod = "vote";


        public bool AlphabetEmit(int index)
        {
            return Invoke(out _, AlphabetContractHash[index], EmitMethod, 0);
        }

        public bool AlphabetVote(int index, ulong epoch, ECPoint[] publicKeys)
        {
            var array = new ContractParameter(ContractParameterType.Array);
            var list = new List<ContractParameter>();
            foreach (var publicKey in publicKeys)
            {
                list.Add(new ContractParameter(ContractParameterType.PublicKey) { Value = publicKey });
            }
            array.Value = list;
            return Invoke(out _, AlphabetContractHash[index], VoteMethod, SideChainFee, epoch, array);
        }

        public void InnerRingIndex(ECPoint key, out int index, out int length)
        {
            ECPoint[] innerRing = NeoFSAlphabetList();
            index = KeyPosition(key, innerRing);
            length = innerRing.Length;
        }

        public int AlphabetIndex(ECPoint key)
        {
            return KeyPosition(key, Committee());
        }

        private int KeyPosition(ECPoint key, ECPoint[] list)
        {
            var result = -1;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].Equals(key))
                {
                    result = i;
                    break;
                }
            }
            return result;
        }
    }
}
