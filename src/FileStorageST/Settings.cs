using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Neo.FileStorage.Storage.LocalObjectStorage.Blob;
using Neo.FileStorage.Storage.LocalObjectStorage.Blobstor;
using Neo.FileStorage.Storage.LocalObjectStorage.Shards;

namespace Neo.FileStorage.Storage
{
    public class Settings
    {
        public static Settings Default { get; private set; }
        public bool AutoStart;
        public UInt160 NetmapContractHash;
        public UInt160 BalanceContractHash;
        public UInt160 ContainerContractHash;
        public UInt160 ReputationContractHash;

        public ulong BasicIncomeRate;
        public long MainChainFee;
        public long SideChainFee;
        public long AuditFee;
        public StorageNodeSettings StorageSettings;
        public List<UInt160> Contracts = new();

        private Settings(IConfigurationSection section)
        {
            AutoStart = section.GetValue("AutoStart", false);

            IConfigurationSection contracts = section.GetSection("Contracts");
            NetmapContractHash = UInt160.Parse(contracts.GetSection("Netmap").Value);
            BalanceContractHash = UInt160.Parse(contracts.GetSection("Balance").Value);
            ContainerContractHash = UInt160.Parse(contracts.GetSection("Container").Value);
            ReputationContractHash = UInt160.Parse(contracts.GetSection("Reputation").Value);

            Contracts.Add(NetmapContractHash);
            Contracts.Add(BalanceContractHash);
            Contracts.Add(ContainerContractHash);

            IConfigurationSection fee = section.GetSection("Fee");
            MainChainFee = fee.GetValue("MainChain", 5000L);
            SideChainFee = fee.GetValue("SideChain", 5000L);

            StorageSettings = StorageNodeSettings.Load(section.GetSection("Storage"));
            if (!StorageSettings.Shards.Any()) StorageSettings = StorageNodeSettings.Default;
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }
    }

    public class WriteCacheSettings
    {
        public string Path;
        public ulong MaxMemorySize;
        public ulong MaxObjectSize;
        public ulong SmallObjectSize;
        public ulong MaxDBSize;

        public static WriteCacheSettings Default { get; private set; }

        static WriteCacheSettings()
        {
            Default = new()
            {
                Path = $"Data_WriteCache_{Guid.NewGuid()}",
                MaxMemorySize = WriteCache.DefaultMemorySize,
                MaxObjectSize = WriteCache.DefaultMaxObjectSize,
                SmallObjectSize = WriteCache.DefaultSmallObjectSize,
                MaxDBSize = 0ul//TODO
            };
        }

        public static WriteCacheSettings Load(IConfigurationSection section)
        {
            WriteCacheSettings settings = new()
            {
                Path = section.GetValue("Path", ""),
                MaxMemorySize = section.GetValue("MaxMemorySize", WriteCache.DefaultMemorySize),
                MaxObjectSize = section.GetValue("MaxObjectSize", WriteCache.DefaultMaxObjectSize),
                SmallObjectSize = section.GetValue("SmallObjectSize", WriteCache.DefaultSmallObjectSize),
                MaxDBSize = section.GetValue("MaxDBSize", 0ul)//TODO
            };
            if (settings.Path == "") throw new FormatException("invalid writecache path");
            return settings;
        }
    }

    public class BlobovniczasSettings
    {
        public ulong Size;
        public int ShallowDepth;
        public int ShallowWidth;
        public int OpenCacheSize;

        public static BlobovniczasSettings Default { get; private set; }

        static BlobovniczasSettings()
        {
            Default = new()
            {
                Size = Blobovnicza.DefaultFullSizeLimit,
                ShallowDepth = BlobovniczaTree.DefaultBlzShallowDepth,
                ShallowWidth = BlobovniczaTree.DefaultBlzShallowWidth,
                OpenCacheSize = BlobovniczaTree.DefaultOpenedCacheSize,
            };
        }

        public static BlobovniczasSettings Load(IConfigurationSection section)
        {
            return new()
            {
                Size = section.GetValue("Size", Blobovnicza.DefaultFullSizeLimit),
                ShallowDepth = section.GetValue("ShallowDepth", BlobovniczaTree.DefaultBlzShallowDepth),
                ShallowWidth = section.GetValue("ShallowWidth", BlobovniczaTree.DefaultBlzShallowWidth),
                OpenCacheSize = section.GetValue("OpenCacheSize", BlobovniczaTree.DefaultOpenedCacheSize)
            };
        }
    }

    public class BlobStorageSettings
    {
        public string Path;
        public bool Compress;
        public int ShallowDepth;
        public ulong SmallSizeLimit;
        public BlobovniczasSettings BlobovniczasSettings;

        public static BlobStorageSettings Default { get; private set; }

        static BlobStorageSettings()
        {
            Default = new()
            {
                Path = $"Data_BlobStorage_{Guid.NewGuid()}",
                Compress = true,
                ShallowDepth = FSTree.DefaultShallowDepth,
                SmallSizeLimit = BlobStorage.DefaultSmallSizeLimit,
                BlobovniczasSettings = BlobovniczasSettings.Default
            };
        }

        public static BlobStorageSettings Load(IConfigurationSection section)
        {
            BlobStorageSettings settings = new()
            {
                Path = section.GetValue("Path", ""),
                Compress = section.GetValue("Compress", true),
                ShallowDepth = section.GetValue("ShallowDepth", FSTree.DefaultShallowDepth),
                SmallSizeLimit = section.GetValue("SmallSizeLimit", BlobStorage.DefaultSmallSizeLimit),
                BlobovniczasSettings = BlobovniczasSettings.Load(section.GetSection("Blobovnicza"))
            };
            if (settings.Path == "") throw new FormatException("invalid blobstorage path");
            return settings;
        }
    }

    public class MetabaseSettings
    {
        public string Path;

        public static MetabaseSettings Default { get; private set; }

        static MetabaseSettings()
        {
            Default = new()
            {
                Path = $"Data_Metabase{Guid.NewGuid()}"
            };
        }

        public static MetabaseSettings Load(IConfigurationSection section)
        {
            MetabaseSettings settings = new()
            {
                Path = section.GetValue("Path", "")
            };
            if (settings.Path == "") throw new FormatException("invalid metabase path");
            return settings;
        }
    }

    public class ShardSettings
    {
        public bool UseWriteCache;
        public int RemoverInterval;
        public int RemoveBatchSize;
        public WriteCacheSettings WriteCacheSettings;
        public BlobStorageSettings BlobStorageSettings;
        public MetabaseSettings MetabaseSettings;

        public static ShardSettings Default { get; private set; }

        static ShardSettings()
        {
            Default = new()
            {
                UseWriteCache = Shard.DefaultUseWriteCache,
                RemoverInterval = Shard.DefaultRemoveInterval,
                RemoveBatchSize = Shard.DefaultRemoveBatchSize,
                WriteCacheSettings = WriteCacheSettings.Default,
                BlobStorageSettings = BlobStorageSettings.Default,
                MetabaseSettings = MetabaseSettings.Default,
            };
        }

        public static ShardSettings Load(IConfigurationSection section)
        {
            ShardSettings settings = new();
            settings.UseWriteCache = section.GetValue("UseWriteCache", Shard.DefaultUseWriteCache);
            if (settings.UseWriteCache)
                settings.WriteCacheSettings = WriteCacheSettings.Load(section.GetSection("WriteCache"));
            settings.RemoverInterval = section.GetValue("RemoverInterval", Shard.DefaultRemoveInterval);
            settings.RemoveBatchSize = section.GetValue("RemoveBatchSize", Shard.DefaultRemoveBatchSize);
            settings.BlobStorageSettings = BlobStorageSettings.Load(section.GetSection("BlobStorage"));
            settings.MetabaseSettings = MetabaseSettings.Load(section.GetSection("Metabase"));
            return settings;
        }
    }

    public class StorageNodeSettings
    {
        public const string DefaultAddress = "/ip4/0.0.0.0/tcp/8080";
        public const int DefaultPort = 8080;
        public string Address;
        public List<string> Attributes;
        public int Port;
        public ShardSettings[] Shards;

        public static StorageNodeSettings Default { get; private set; }

        static StorageNodeSettings()
        {
            Default = new()
            {
                Address = DefaultAddress,
                Attributes = new(),
                Port = DefaultPort,
                Shards = new ShardSettings[] { ShardSettings.Default },
            };
        }

        public static StorageNodeSettings Load(IConfigurationSection section)
        {
            return new()
            {
                Address = section.GetValue("Address", DefaultAddress),
                Attributes = section.GetSection("Attributes").GetChildren().Select(p => p.ToString()).ToList(),
                Port = section.GetValue("Port", DefaultPort),
                Shards = section.GetSection("Shards").GetChildren().Select(p => ShardSettings.Load(p)).ToArray(),
            };
        }
    }
}
