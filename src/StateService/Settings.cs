using Microsoft.Extensions.Configuration;
using System.Linq;

namespace Neo.Plugins.StateService
{
    internal class Settings
    {
        public string Path { get; }
        public bool FullState { get; }
        public string[] VerifierUrls { get; }

        public static Settings Default { get; private set; }

        private Settings(IConfigurationSection section)
        {
            Path = string.Format(section.GetValue("Path", "Data_MPT_{0}"), ProtocolSettings.Default.Magic.ToString("X8"));
            FullState = section.GetValue("FullState", false);
            VerifierUrls = section.GetSection("VerifierUrls").GetChildren().Select(p => p.Get<string>()).ToArray();
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }
    }
}
