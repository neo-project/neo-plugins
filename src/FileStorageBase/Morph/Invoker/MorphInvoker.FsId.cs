using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Neo.FileStorage.API.Reputation;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM.Types;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.FileStorage.Morph.Invoker
{
    public partial class MorphInvoker
    {
        private const string KeyListingMethod = "key";
        private const string AddKeysMethod = "addKey";
        private const string RemoveKeysMethod = "removeKey";

        public ECPoint[] AccountKeys(byte[] owner)
        {
            InvokeResult result = TestInvoke(FsIdContractHash, KeyListingMethod, owner);
            if (result.State != VM.VMState.HALT) throw new Exception($"could not perform test invocation ({KeyListingMethod})");
            if (result.ResultStack.Length != 1) throw new Exception();
            VM.Types.Array items = (VM.Types.Array)result.ResultStack[0];
            IEnumerator<StackItem> itemsEnumerator = items.GetEnumerator();
            List<ECPoint> lists = new();
            while (itemsEnumerator.MoveNext())
            {
                lists.Add(itemsEnumerator.Current.GetSpan().AsSerializable<ECPoint>());
            }
            return lists.ToArray();
        }

        public bool AddKeys(UInt160 owner, ECPoint[] keys)
        {
            var array = new ContractParameter(ContractParameterType.Array);
            var list = new List<ContractParameter>();
            foreach (var publicKey in keys)
            {
                list.Add(new ContractParameter(ContractParameterType.PublicKey) { Value = publicKey });
            }
            array.Value = list;
            return Invoke(out _, FsIdContractHash, AddKeysMethod, SideChainFee, owner, array);
        }

        public bool RemoveKeys(UInt160 owner, ECPoint[] keys)
        {
            var array = new ContractParameter(ContractParameterType.Array);
            var list = new List<ContractParameter>();
            foreach (var publicKey in keys)
            {
                list.Add(new ContractParameter(ContractParameterType.PublicKey) { Value = publicKey });
            }
            array.Value = list;
            return Invoke(out _, FsIdContractHash, RemoveKeysMethod, SideChainFee, owner, array);
        }
    }
}
