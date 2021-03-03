using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace Neo.Plugins
{
    class HttpsSettings
    {
        public TimeSpan Timeout { get; }

        public HttpsSettings(IConfigurationSection section)
        {
            Timeout = TimeSpan.FromMilliseconds(section.GetValue("Timeout", 5000));
        }
    }

    class NeoFSSettings
    {
        public string EndPoint { get; }

        public NeoFSSettings(IConfigurationSection section)
        {
            EndPoint = section.GetValue("EndPoint", "127.0.0.1:8080");
        }
    }

    class Settings
    {
        public uint Network { get; }
        public Uri[] Nodes { get; }
        public TimeSpan MaxTaskTimeout { get; }
        public bool AllowPrivateHost { get; }
        public string[] AllowedContentTypes { get; }
        public HttpsSettings Https { get; }
        public NeoFSSettings NeoFS { get; }

        public bool AutoStart { get; }

        public static Settings Default { get; private set; }

        private Settings(IConfigurationSection section)
        {
            Network = section.GetValue("Network", 5195086u);
            Nodes = section.GetSection("Nodes").GetChildren().Select(p => new Uri(p.Get<string>(), UriKind.Absolute)).ToArray();
            MaxTaskTimeout = TimeSpan.FromMilliseconds(section.GetValue("MaxTaskTimeout", 432000000));
            AllowPrivateHost = section.GetValue("AllowPrivateHost", false);
            AllowedContentTypes = section.GetSection("AllowedContentTypes").GetChildren().Select(p => p.Get<string>()).ToArray();
            Https = new HttpsSettings(section.GetSection("Https"));
            NeoFS = new NeoFSSettings(section.GetSection("NeoFS"));
            AutoStart = section.GetValue("AutoStart", false);
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }
    }
}
