using Neo.Ledger;
using System;

namespace Neo.Plugins
{
    public static class TestBlockchain
    {
        public static readonly NeoSystem TheNeoSystem;

        static TestBlockchain()
        {
            Console.WriteLine("initialize NeoSystem");
            TheNeoSystem = new NeoSystem(ProtocolSettings.Default, null, null);
        }

        public static void InitializeMockNeoSystem()
        {
        }
    }
}
