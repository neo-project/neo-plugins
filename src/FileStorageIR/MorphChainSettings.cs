using System.Threading;
using Microsoft.Extensions.Configuration;
using Neo.Network.P2P;

namespace Neo.FileStorage.InnerRing
{
    public class MorphChainSettings
    {
        public bool VerifyImport { get; }
        public StorageSettings Storage { get; }
        public P2PSettings P2P { get; }

        public static MorphChainSettings Load(string path, bool optional = true)
        {
            IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile(path, optional: optional).Build();
            return new MorphChainSettings(config.GetSection("ApplicationConfiguration"));
        }

        public MorphChainSettings(IConfigurationSection section)
        {
            VerifyImport = section.GetValue("VerifyImport", true);
            Storage = new StorageSettings(section.GetSection("Storage"));
            P2P = new P2PSettings(section.GetSection("P2P"));
        }
    }

    public class StorageSettings
    {
        public string Engine { get; }
        public string Path { get; }

        public StorageSettings(IConfigurationSection section)
        {
            Engine = section.GetValue("Engine", "LevelDBStore");
            Path = section.GetValue("Path", "Data_LevelDB_Side");
        }
    }

    public class P2PSettings
    {
        public ushort Port { get; }
        public ushort WsPort { get; }
        public int MinDesiredConnections { get; }
        public int MaxConnections { get; }
        public int MaxConnectionsPerAddress { get; }

        public P2PSettings(IConfigurationSection section)
        {
            Port = ushort.Parse(section.GetValue("Port", "30333"));
            WsPort = ushort.Parse(section.GetValue("WsPort", "30334"));
            MinDesiredConnections = section.GetValue("MinDesiredConnections", Peer.DefaultMinDesiredConnections);
            MaxConnections = section.GetValue("MaxConnections", Peer.DefaultMaxConnections);
            MaxConnectionsPerAddress = section.GetValue("MaxConnectionsPerAddress", 3);
        }
    }
}
