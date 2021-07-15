using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Neo.Cryptography.ECC;
using Neo.FileStorage.API.Container;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Morph.Invoker;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using ByteString = Google.Protobuf.ByteString;

namespace Neo.FileStorage.Tests
{
    public static class TestBlockchain
    {
        private const string walletPath = "./Config/wallet-all.json";
        private const string password = "123456";
        public static UInt160 ReputationContractHash { get; private set; }
        public static UInt160 BalanceContractHash { get; private set; }
        public static UInt160 AuditContractHash { get; private set; }
        public static UInt160 ContainerContractHash { get; private set; }
        public static UInt160 FsIdContractHash { get; private set; }
        public static UInt160 FsContractHash { get; private set; }
        public static UInt160 NetmapContractHash { get; private set; }
        public static UInt160 ProcessContractHash { get; private set; }
        public static UInt160 ProxyContractHash { get; private set; }
        public static UInt160[] AlphabetContractHash;
        public static readonly List<UInt160> Contracts = new();
        public static readonly NeoSystem TheNeoSystem;
        public static NEP6Wallet wallet;
        public static ProtocolSettings protocolSettings;

        static TestBlockchain()
        {
            string ConfigFilePath = "./Config/ProtocolSettingsConfig.json";
            protocolSettings = ProtocolSettings.Load(ConfigFilePath);
            TheNeoSystem = new NeoSystem(protocolSettings);
            Console.WriteLine("initialize NeoSystem");
            InitializeMockNeoSystem();
        }

        public static void InitializeMockNeoSystem()
        {
            NeoSystem system = TheNeoSystem;
            wallet = new NEP6Wallet(walletPath, TheNeoSystem.Settings);
            wallet.Unlock(password);
            if (!File.Exists(walletPath))
            {
                for (int i = 0; i < 7; i++)
                {
                    wallet.CreateAccount();
                }
                wallet.Save();
            }
            //Fake balance
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            UInt160 from = Contract.GetBFTAddress(TheNeoSystem.Settings.StandbyValidators);
            UInt160 to = accounts.ToArray()[0].ScriptHash;
            FakeSigners signers = new FakeSigners(from);
            byte[] script = NativeContract.GAS.Hash.MakeScript("transfer", from, to, 500_00000000, null);
            using var snapshot = TheNeoSystem.GetSnapshot();
            ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, container: signers, null, TheNeoSystem.Settings, 0, 2000000000);
            //Fake deploy contract
            string DeployContractsCasesPath = "./Config/Contracts/DeployContractCases.json";
            IEnumerable<IConfigurationSection> deployContracts = new ConfigurationBuilder().AddJsonFile(DeployContractsCasesPath, optional: true).Build().GetSection("DeployContracts").GetChildren();
            UInt160 ProcessContractHash = null;
            UInt160 ProxyContractHash = null;
            foreach (IConfigurationSection deployContract in deployContracts)
            {
                string contractName = deployContract.GetSection("Name").Value;
                string nefFilePath = deployContract.GetSection("NefFilePath").Value;
                string manifestPath = deployContract.GetSection("ManifestPath").Value;
                var manifest = ContractManifest.Parse(File.ReadAllBytes(manifestPath));
                NefFile nef;
                using (var stream = new BinaryReader(File.OpenRead(nefFilePath), Utility.StrictUTF8, false))
                {
                    nef = stream.ReadSerializable<NefFile>();
                }
                UInt160 sender = accounts.ToArray()[int.Parse(deployContract.GetSection("accountIndex").Value)].ScriptHash;
                UInt160 contractHash = SmartContract.Helper.GetContractHash(sender, nef.CheckSum, manifest.Name);
                switch (manifestPath)
                {
                    case "./Config/Contracts/reputation/config.json":
                        ReputationContractHash = contractHash;
                        Contracts.Add(contractHash);
                        break;
                    case "./Config/Contracts/balance/config.json":
                        BalanceContractHash = contractHash;
                        Contracts.Add(contractHash);
                        break;
                    case "./Config/Contracts/audit/config.json":
                        AuditContractHash = contractHash;
                        Contracts.Add(contractHash);
                        break;
                    case "./Config/Contracts/container/config.json":
                        ContainerContractHash = contractHash;
                        Contracts.Add(contractHash);
                        break;
                    case "./Config/Contracts/neofs/config.json":
                        FsContractHash = contractHash;
                        Contracts.Add(contractHash);
                        break;
                    case "./Config/Contracts/neofsid/config.json":
                        FsIdContractHash = contractHash;
                        Contracts.Add(contractHash);
                        break;
                    case "./Config/Contracts/netmap/config.json":
                        NetmapContractHash = contractHash;
                        Contracts.Add(contractHash);
                        break;
                    case "./Config/Contracts/processing/config.json":
                        ProcessContractHash = contractHash;
                        break;
                    case "./Config/Contracts/proxy/config.json":
                        ProxyContractHash = contractHash;
                        break;
                    default:
                        AlphabetContractHash = AlphabetContractHash.Append(contractHash).ToArray();
                        Contracts.Add(contractHash);
                        break;
                }
            }
            foreach (IConfigurationSection deployContract in deployContracts)
            {
                string contractName = deployContract.GetSection("Name").Value;
                string nefFilePath = deployContract.GetSection("NefFilePath").Value;
                string manifestPath = deployContract.GetSection("ManifestPath").Value;
                var manifest = ContractManifest.Parse(File.ReadAllBytes(manifestPath));
                NefFile nef;
                using (var stream = new BinaryReader(File.OpenRead(nefFilePath), Utility.StrictUTF8, false))
                {
                    nef = stream.ReadSerializable<NefFile>();
                }
                UInt160 sender = accounts.ToArray()[int.Parse(deployContract.GetSection("accountIndex").Value)].ScriptHash;
                UInt160 contractHash = SmartContract.Helper.GetContractHash(sender, nef.CheckSum, manifest.Name);
                var data = new List<ContractParameter>();
                switch (manifestPath)
                {
                    case "./Config/Contracts/reputation/config.json":
                        data.Clear();
                        data.Add(new ContractParameter(ContractParameterType.Boolean) { Value = true });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = sender });
                        break;
                    case "./Config/Contracts/balance/config.json":
                        data.Clear();
                        data.Add(new ContractParameter(ContractParameterType.Boolean) { Value = true });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = sender });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = NetmapContractHash });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = ContainerContractHash });
                        break;
                    case "./Config/Contracts/audit/config.json":
                        data.Clear();
                        data.Add(new ContractParameter(ContractParameterType.Boolean) { Value = true });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = sender });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = NetmapContractHash });
                        break;
                    case "./Config/Contracts/container/config.json":
                        data.Clear();
                        data.Add(new ContractParameter(ContractParameterType.Boolean) { Value = true });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = sender });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = NetmapContractHash });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = BalanceContractHash });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = FsIdContractHash });
                        break;
                    case "./Config/Contracts/neofs/config.json":
                        data.Clear();
                        data.Add(new ContractParameter(ContractParameterType.Boolean) { Value = true });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = sender });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = ProcessContractHash });
                        var keys = new List<ContractParameter>();
                        accounts.ToList().ForEach(p => keys.Add(new ContractParameter(ContractParameterType.PublicKey) { Value = p.GetKey().PublicKey }));
                        data.Add(new ContractParameter(ContractParameterType.Array) { Value = keys });
                        break;
                    case "./Config/Contracts/neofsid/config.json":
                        data.Clear();
                        data.Add(new ContractParameter(ContractParameterType.Boolean) { Value = true });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = sender });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = NetmapContractHash });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = ContainerContractHash });
                        break;
                    case "./Config/Contracts/netmap/config.json":
                        data.Clear();
                        data.Add(new ContractParameter(ContractParameterType.Boolean) { Value = true });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = sender });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = BalanceContractHash });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = ContainerContractHash });
                        keys = new List<ContractParameter>();
                        accounts.ToList().ForEach(p => keys.Add(new ContractParameter(ContractParameterType.PublicKey) { Value = p.GetKey().PublicKey }));
                        data.Add(new ContractParameter(ContractParameterType.Array) { Value = keys });
                        break;
                    case "./Config/Contracts/processing/config.json":
                        data.Clear();
                        data.Add(new ContractParameter(ContractParameterType.Boolean) { Value = true });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = sender });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = FsContractHash });
                        break;
                    case "./Config/Contracts/proxy/config.json":
                        data.Clear();
                        data.Add(new ContractParameter(ContractParameterType.Boolean) { Value = true });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = sender });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = NetmapContractHash });
                        break;
                    default:
                        data.Clear();
                        data.Add(new ContractParameter(ContractParameterType.Boolean) { Value = true });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = sender });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = NetmapContractHash });
                        data.Add(new ContractParameter(ContractParameterType.Hash160) { Value = ProxyContractHash });
                        data.Add(new ContractParameter(ContractParameterType.String) { Value = "Alphabet" + int.Parse(deployContract.GetSection("accountIndex").Value) });
                        data.Add(new ContractParameter(ContractParameterType.Integer) { Value = int.Parse(deployContract.GetSection("accountIndex").Value) });
                        data.Add(new ContractParameter(ContractParameterType.Integer) { Value = AlphabetContractHash.Length });
                        break;
                }
                DeployContract(snapshot, contractName, nefFilePath, manifestPath, new ContractParameter(ContractParameterType.Array) { Value = data }, sender);
            }

            //Fake contract init
            //FakeNetMapConfigInit/ContainerFee
            script = NetmapContractHash.MakeScript("initConfig", ToParameter(new byte[][] { Utility.StrictUTF8.GetBytes("ContainerFee"), BitConverter.GetBytes(0) }));
            ExecuteScript(snapshot, "NetMapConfigInit/ContainerFee", script, from);
            //Fake others
            //Fake peer
            //Fake IR
            script = NativeContract.RoleManagement.Hash.MakeScript("designateAsRole", Role.NeoFSAlphabetNode, ToParameter(accounts.Select(p => p.GetKey().PublicKey.ToArray()).ToArray()));
            ExecuteScript(snapshot, "FakeIR", script, NativeContract.NEO.GetCommitteeAddress(snapshot));
            NodeInfo nodeInfo = new NodeInfo();
            nodeInfo.Address = API.Cryptography.KeyExtension.PublicKeyToAddress(accounts.ToArray()[0].GetKey().PublicKey.ToArray());
            nodeInfo.PublicKey = ByteString.CopyFrom(accounts.ToArray()[0].GetKey().PublicKey.ToArray());
            var rawNodeInfo = nodeInfo.ToByteArray();
            script = NetmapContractHash.MakeScript("addPeer", rawNodeInfo);
            for (int i = 0; i < accounts.Count(); i++)
            {
                ExecuteScript(snapshot, "FakePeer", script, accounts.ToArray()[i].ScriptHash);
            }
            //Fake container
            KeyPair key = accounts.ToArray()[0].GetKey();
            API.Refs.OwnerID ownerId = KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new API.Refs.Version(),
                BasicAcl = 0,
                Nonce = ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new PlacementPolicy()
            };
            byte[] sig = key.PrivateKey.LoadPrivateKey().SignRFC6979(container.ToByteArray());
            script = ContainerContractHash.MakeScript("put", container.ToByteArray(), sig, key.PublicKey.ToArray(), null);
            var containerId = container.CalCulateAndGetId.Value.ToByteArray();
            for (int i = 0; i < accounts.Count(); i++)
            {
                ExecuteScript(snapshot, "FakeContainer", script, accounts.ToArray()[i].ScriptHash);
            }
            Console.WriteLine("FakeContainerID:" + containerId.ToHexString());
            //Fake eacl
            API.Acl.EACLTable eACLTable = new API.Acl.EACLTable()
            {
                ContainerId = container.CalCulateAndGetId,
                Version = new API.Refs.Version(),
            };
            eACLTable.Records.Add(new API.Acl.EACLRecord());
            sig = Cryptography.Crypto.Sign(eACLTable.ToByteArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            script = ContainerContractHash.MakeScript("setEACL", eACLTable.ToByteArray(), sig, key.PublicKey.EncodePoint(false), null);
            for (int i = 0; i < accounts.Count(); i++)
            {
                ExecuteScript(snapshot, "FakeEacl", script, accounts.ToArray()[i].ScriptHash);
            }
            //Fake Epoch
            for (int i = 0; i < accounts.Count(); i++)
            {
                script = NetmapContractHash.MakeScript("newEpoch", 1);
                ExecuteScript(snapshot, "FakeEpoch", script, accounts.ToArray()[i].ScriptHash);
            }
            NodeList list = new();
            list.AddRange(accounts.Select(p => p.GetKey().PublicKey));
            list.Sort();
            snapshot.Add(new KeyBuilder(NativeContract.RoleManagement.Id, (byte)Role.NeoFSAlphabetNode).AddBigEndian(0), new StorageItem(list));
            for (int i = 0; i < 7; i++)
            {
                script = NativeContract.GAS.Hash.MakeScript("transfer", from, AlphabetContractHash[i], 500_00000000, null);
                engine = ApplicationEngine.Run(script, snapshot, container: signers, null, TheNeoSystem.Settings, 0, 2000000000);
            }
            snapshot.Commit();
        }

        private class NodeList : List<ECPoint>, IInteroperable
        {
            public void FromStackItem(StackItem stackItem)
            {
                foreach (StackItem item in (VM.Types.Array)stackItem)
                    Add(item.GetSpan().AsSerializable<ECPoint>());
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                return new VM.Types.Array(referenceCounter, this.Select(p => (StackItem)p.ToArray()));
            }
        }

        private static byte[] MakeScript(UInt160 scriptHash, string operation, byte[][] args)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                for (int i = args.Length - 1; i >= 0; i--)
                {
                    sb.EmitPush(args[i]);
                }
                sb.EmitPush(args.Length);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(1);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(operation);
                sb.EmitPush(scriptHash);
                sb.EmitSysCall(ApplicationEngine.System_Contract_Call);
                return sb.ToArray();
            }
        }

        public static ContractParameter ToParameter(byte[][] args)
        {
            var array = new ContractParameter(ContractParameterType.Array);
            var list = new List<ContractParameter>();
            foreach (var bytes in args)
            {
                list.Add(new ContractParameter(ContractParameterType.ByteArray) { Value = bytes });
            }
            array.Value = list;
            return array;
        }

        public static void ContractAdd(DataCache snapshot, ContractState contract)
        {
            var key = new KeyBuilder(-1, 8).Add(contract.Hash);
            snapshot.Add(key, new StorageItem(contract));
        }

        private static void DeployContract(DataCache snapshot, string contractName, string nefFilePath, string manifestFilePath, ContractParameter data, UInt160 sender)
        {
            var manifest = ContractManifest.Parse(File.ReadAllBytes(manifestFilePath));
            NefFile nef;
            using (var stream = new BinaryReader(File.OpenRead(nefFilePath), Neo.Utility.StrictUTF8, false))
            {
                nef = stream.ReadSerializable<NefFile>();
            }
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", nef.ToArray(), manifest.ToJson().ToString(), data);
            Random rand = new Random();
            Transaction tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)rand.Next(),
                Script = sb.ToArray(),
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + TheNeoSystem.Settings.MaxValidUntilBlockIncrement,
                Signers = new Signer[] { new Signer() { Account = sender, Scopes = WitnessScope.Global } },
                Attributes = System.Array.Empty<TransactionAttribute>(),
            };
            ApplicationEngine engine = ApplicationEngine.Run(sb.ToArray(), snapshot, tx, null, TheNeoSystem.Settings, 0, 2000000000);

            if (engine.State != VMState.HALT)
            {
                var contractHash = UInt160.Zero;
                Console.WriteLine($"Deploy {contractName} contract fault.Contract Hash:{contractHash}");
            }
            else
            {
                var contractHash = new UInt160(((VM.Types.Array)engine.ResultStack.Peek())[2].GetSpan());
                Console.WriteLine($"Deploy {contractName} contract success.Contract Hash:{contractHash}");
            }
        }

        private static void ExecuteScript(DataCache snapshot, string functionName, byte[] script, UInt160 sender)
        {
            FakeSigners signers = new FakeSigners(sender);
            ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, container: signers, TheNeoSystem.GenesisBlock, TheNeoSystem.Settings, 0, 2000000000);
            var faultMessage = engine.FaultException?.ToString();
            var innerMessage = engine.FaultException?.InnerException?.ToString();
            var errorMessage = $"\r\nException:{faultMessage}";
            if (!string.IsNullOrWhiteSpace(innerMessage))
            {
                errorMessage += "\r\n\tInner:" + innerMessage;
            }

            if (engine.State != VMState.HALT)
                Console.WriteLine($"{functionName} execute fault.{errorMessage}");
            else
                Console.WriteLine($"{functionName} execute success.");
        }
    }

    public class MyWallet : Wallet
    {
        public string path;

        public override string Name => "MyWallet";

        public override Version Version => Version.Parse("0.0.1");

        Dictionary<UInt160, WalletAccount> accounts = new Dictionary<UInt160, WalletAccount>();

        public MyWallet(string path) : base(path, ProtocolSettings.Default)
        {
        }

        public override bool ChangePassword(string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(UInt160 scriptHash)
        {
            return accounts.ContainsKey(scriptHash);
        }

        public void AddAccount(WalletAccount account)
        {
            accounts.Add(account.ScriptHash, account);
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            KeyPair key = new KeyPair(privateKey);
            Contract contract = new Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            MyWalletAccount account = new MyWalletAccount(contract.ScriptHash);
            account.SetKey(key);
            account.Contract = contract;
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair key = null)
        {
            MyWalletAccount account = new MyWalletAccount(contract.ScriptHash)
            {
                Contract = contract
            };
            account.SetKey(key);
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            MyWalletAccount account = new MyWalletAccount(scriptHash);
            AddAccount(account);
            return account;
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            return accounts.Remove(scriptHash);
        }

        public override WalletAccount GetAccount(UInt160 scriptHash)
        {
            accounts.TryGetValue(scriptHash, out WalletAccount account);
            return account;
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            return accounts.Values;
        }

        public override bool VerifyPassword(string password)
        {
            return true;
        }
    }

    public class MyWalletAccount : WalletAccount
    {
        private KeyPair key = null;
        public override bool HasKey => key != null;

        public MyWalletAccount(UInt160 scriptHash)
            : base(scriptHash, ProtocolSettings.Default)
        {
        }

        public override KeyPair GetKey()
        {
            return key;
        }

        public void SetKey(KeyPair inputKey)
        {
            key = inputKey;
        }
    }
}
