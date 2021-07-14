using Akka.Actor;
using Neo.ConsoleService;
using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Linq;
using static System.IO.Path;

namespace Neo.Plugins.MultiSigInbox
{
    public class MultiSigInboxPlugin : Plugin, IP2PPlugin
    {
        public const string MutiSigPayloadCategory = "MultiSigInbox";
        public override string Name => "MultiSigInbox";
        public override string Description => "Enables MultiSigInbox for the node";

        internal static readonly byte[] TxPrefix = new byte[] { 0x01 };

        private Wallet _wallet;
        private IWalletProvider walletProvider;
        private IActorRef service;
        private bool started = false;
        private NeoSystem System;
        private IStore store;

        protected override void Configure()
        {
            Settings.Load(GetConfiguration());
        }

        public override void Dispose()
        {
            base.Dispose();
            store?.Dispose();
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (system.Settings.Magic != Settings.Default.Network) return;
            System = system;
            System.ServiceAdded += NeoSystem_ServiceAdded;
            string path = string.Format(Settings.Default.Path, system.Settings.Magic.ToString("X8"));
            store = system.LoadStore(GetFullPath(path));
        }

        private void NeoSystem_ServiceAdded(object sender, object service)
        {
            if (service is IWalletProvider)
            {
                walletProvider = service as IWalletProvider;
                System.ServiceAdded -= NeoSystem_ServiceAdded;
                if (Settings.Default.AutoStart)
                {
                    walletProvider.WalletChanged += WalletProvider_WalletChanged;
                }
            }
        }

        private void WalletProvider_WalletChanged(object sender, Wallet wallet)
        {
            walletProvider.WalletChanged -= WalletProvider_WalletChanged;
            Start(wallet);
        }

        [ConsoleCommand("start multisiginbox", Category = "MultiSigInbox", Description = "Start multiSigInbox service")]
        private void OnStart()
        {
            Start(walletProvider.GetWallet());
        }

        public void Start(Wallet wallet)
        {
            if (started) return;
            started = true;
            _wallet = wallet;
            service = System.ActorSystem.ActorOf(MultiSigInboxService.Props(System, store, wallet));
            service.Tell(new MultiSigInboxService.Start());
        }

        [ConsoleCommand("inbox list", Category = "MultiSigInbox", Description = "List pending transactions")]
        private void OnInboxList()
        {
            var first = true;
            using var snapshot = System.GetSnapshot();

            foreach (var (k, v) in store.Seek(TxPrefix, SeekDirection.Forward))
            {
                ContractParametersContext context = ContractParametersContext.FromJson(JObject.Parse(v), snapshot);
                if (first)
                {
                    first = false;
                    Console.WriteLine($"Transaction hashes");
                }
                Console.WriteLine($"{context.Verifiable.Hash}");
            }
            if (first)
            {
                Console.WriteLine($"No entries was found");
            }
        }

        [ConsoleCommand("inbox read", Category = "MultiSigInbox", Description = "Read transaction")]
        private void OnInboxRead(UInt256 txHash)
        {
            var json = store.TryGet(TxPrefix.Concat(txHash.ToArray()).ToArray());
            if (json != null)
            {
                Console.WriteLine(Utility.StrictUTF8.GetString(json));
            }
            else
            {
                Console.WriteLine("Transaction was not found");
            }
        }

        [ConsoleCommand("inbox send", Category = "MultiSigInbox", Description = "Send transaction")]
        private void OnInboxSend(string transaction)
        {
            ContractParametersContext context = ContractParametersContext.FromJson(JObject.Parse(transaction), System.StoreView);

            var tx = store.TryGet(TxPrefix.Concat(context.Verifiable.Hash.ToArray()).ToArray());
            if (tx != null)
            {
                Console.WriteLine("Transaction already sent");
                return;
            }

            store.Put(TxPrefix.Concat(context.Verifiable.Hash.ToArray()).ToArray(), Utility.StrictUTF8.GetBytes(transaction));

            using var snapshot = System.GetSnapshot();
            RelayContext(snapshot, context);
        }

        [ConsoleCommand("inbox sign", Category = "MultiSigInbox", Description = "Sign transaction")]
        private void OnInboxSign(UInt256 txHash)
        {
            var tx = store.TryGet(TxPrefix.Concat(txHash.ToArray()).ToArray());
            if (tx != null)
            {
                using var snapshot = System.GetSnapshot();
                var context = ContractParametersContext.FromJson(JObject.Parse(tx), snapshot);
                if (context.Verifiable.Witnesses is null)
                {
                    context.Verifiable.Witnesses ??= Array.Empty<Witness>();
                    Console.WriteLine((context.Verifiable as Transaction).ToJson(System.Settings).ToString(true));
                    context.Verifiable.Witnesses = null;
                }
                else
                {
                    Console.WriteLine((context.Verifiable as Transaction).ToJson(System.Settings).ToString(true));
                }
                RelayContext(snapshot, context);
            }
            else
            {
                Console.WriteLine("Transaction was not found");
            }
        }

        private void RelayContext(SnapshotCache snapshot, ContractParametersContext context)
        {
            if (context.Completed)
            {
                Log($"Send tx: hash={context.Verifiable.Hash}");
                context.Verifiable.Witnesses = context.GetWitnesses();
                System.Blockchain.Tell(context.Verifiable);
            }
            else
            {
                var tx = store.TryGet(TxPrefix.Concat(context.Verifiable.Hash.ToArray()).ToArray());

                foreach (var wallet in _wallet.GetAccounts())
                {
                    var msg = new ExtensiblePayload()
                    {
                        Category = MutiSigPayloadCategory,
                        Data = tx,
                        ValidBlockStart = 0,
                        ValidBlockEnd = NativeContract.Ledger.CurrentIndex(snapshot) + 1_000,
                        Sender = wallet.ScriptHash
                    };

                    var sign = new ContractParametersContext(snapshot, msg);
                    if (_wallet.Sign(sign) && sign.Completed)
                    {
                        msg.Witness = sign.GetWitnesses()[0];
                        System.Blockchain.Tell(sign.Verifiable);
                        Console.WriteLine($"TXID: {context.Verifiable.Hash}");
                        break;
                    }
                }
            }
        }
    }
}
