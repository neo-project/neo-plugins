using Akka.Actor;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.StateService.Storage;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.StateService.Validation
{
    public class ValidationProcess
    {
        private StateRoot root;
        private ExtensiblePayload payload;
        private readonly Wallet wallet;
        private KeyPair keyPair;
        private int myIndex;
        private uint rootIndex;
        private ECPoint[] validators;
        private int M => validators.Length - (validators.Length - 1) / 3;
        private readonly Dictionary<int, byte[]> signatures = new Dictionary<int, byte[]>();

        public bool IsValidator => myIndex >= 0;
        public int MyIndex => myIndex;
        public ICancelable Timer;
        public StateRoot StateRoot
        {
            get
            {
                if (root is null)
                {
                    using var snapshot = StateStore.Singleton.GetSnapshot();
                    root = snapshot.GetStateRoot(rootIndex);
                }
                return root;
            }
        }
        public ExtensiblePayload Message => payload;

        public ValidationProcess() { }

        public ValidationProcess(Wallet wallet, uint index)
        {
            this.wallet = wallet;
            Initialize(index);
        }

        public void Initialize(uint index)
        {
            myIndex = -1;
            root = null;
            rootIndex = index;
            using SnapshotView snapshot = Blockchain.Singleton.GetSnapshot();
            validators = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.StateValidator, index);
            for (int i = 0; i < validators.Length; i++)
            {
                WalletAccount account = wallet?.GetAccount(validators[i]);
                if (account?.HasKey != true) continue;
                myIndex = i;
                keyPair = account.GetKey();
                break;
            }
        }

        public Vote CreateVote()
        {
            if (StateRoot is null) return null;
            if (!signatures.TryGetValue(myIndex, out byte[] sig))
            {
                sig = StateRoot.Sign(keyPair);
                signatures[myIndex] = sig;
            }
            return new Vote(rootIndex, myIndex, sig);
        }

        public bool AddSignature(int index, byte[] sig)
        {
            if (M <= signatures.Count) return false;
            if (index < 0 || validators.Length <= index) return false;
            if (signatures.ContainsKey(index)) return false;
            Utility.Log(nameof(ValidationService), LogLevel.Info, $"vote received, index={index}");
            ECPoint validator = validators[index];
            byte[] hash_data = StateRoot?.GetHashData();
            if (hash_data != null && !Crypto.VerifySignature(hash_data, sig, validator))
            {
                Utility.Log(nameof(ValidationService), LogLevel.Info, "incorrect vote, invalid signature");
                return false;
            }
            signatures.Add(index, sig);
            return true;
        }

        private void CreateStateRoot()
        {
            Contract contract = Contract.CreateMultiSigContract(M, validators);
            ContractParametersContext sc = new ContractParametersContext(StateRoot);
            for (int i = 0, j = 0; i < validators.Length && j < M; i++)
            {
                bool ok = signatures.TryGetValue(i, out byte[] sig);
                if (!ok) continue;
                sc.AddSignature(contract, validators[i], sig);
                j++;
            }
            StateRoot.Witness = sc.GetWitnesses()[0];
        }

        private void CreateExtensiblePayload()
        {
            payload = new ExtensiblePayload
            {
                Category = StatePlugin.StatePayloadCategory,
                ValidBlockStart = StateRoot.Index,
                ValidBlockEnd = uint.MaxValue,
                Sender = keyPair.PublicKeyHash,
                Data = StateRoot.ToArray(),
            };
            ContractParametersContext sc;
            try
            {
                sc = new ContractParametersContext(payload);
                wallet.Sign(sc);
            }
            catch (InvalidOperationException)
            {
                return;
            }
            payload.Witness = sc.GetWitnesses()[0];
        }

        public bool CheckSignatures()
        {
            if (StateRoot is null) return false;
            if (signatures.Count < M) return false;
            if (StateRoot.Witness != null) return true;
            CreateStateRoot();
            CreateExtensiblePayload();
            return true;
        }
    }
}
