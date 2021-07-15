using System;
using System.Collections.Generic;
using Neo.ConsoleService;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.Wallets;

namespace Neo.FileStorage.Storage
{
    /// <summary>
    /// The entrance of the FSNode program.
    /// Built-in an innering service to process notification events related to FS when the block is persisted.
    /// </summary>
    public partial class StoragePlugin : Plugin, IPersistencePlugin
    {
        public event EventHandler<Wallet> WalletChanged;
        public override string Name => "StorageService";
        public override string Description => "Provide distributed file storage service";

        public NeoSystem MorphSystem;
        public StorageService StorageService;
        private IWalletProvider walletProvider;

        protected override void Configure()
        {
            Settings.Load(GetConfiguration());
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (MorphSystem is null)
            {
                MorphSystem = system;
                MorphSystem.ServiceAdded += NeoSystem_ServiceAdded;
            }
        }

        public void OnPersist(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            if (system.Settings.Network == MorphSystem?.Settings.Network)
            {
                StorageService?.OnPersisted(block, snapshot, applicationExecutedList);
            }
        }

        private void NeoSystem_ServiceAdded(object sender, object service)
        {
            if (service is IWalletProvider)
            {
                walletProvider = service as IWalletProvider;
                MorphSystem.ServiceAdded -= NeoSystem_ServiceAdded;
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

        private void Start(Wallet wallet)
        {
            if (MorphSystem is null) throw new InvalidOperationException("Neo system not initialized");
            if (StorageService is not null) throw new InvalidOperationException("Storage service already started");
            if (wallet is null) throw new InvalidOperationException("Please open wallet first");
            StorageService = new(wallet, MorphSystem);
        }

        [ConsoleCommand("fs start storage", Category = "FileStorageService", Description = "Start as storage node")]
        private void OnStartStorage()
        {
            Start(walletProvider?.GetWallet());
        }

        public override void Dispose()
        {
            base.Dispose();
            StorageService?.Dispose();
        }
    }
}
