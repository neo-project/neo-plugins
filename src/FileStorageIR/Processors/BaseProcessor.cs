using System.Numerics;
using Akka.Actor;
using Neo.FileStorage.InnerRing.Invoker;
using Neo.FileStorage.Morph.Event;
using Neo.FileStorage.Morph.Invoker;

namespace Neo.FileStorage.InnerRing.Processors
{
    public class BaseProcessor : IProcessor
    {
        public static BigInteger bigGB = new BigInteger(1 << 30);
        public static BigInteger bigZero = new BigInteger(0);
        public static BigInteger bigOne = new BigInteger(1);

        public virtual string Name => "BaseProcessor";
        public UInt160 ContainerContractHash => Settings.Default.ContainerContractHash;
        public UInt160 FsContractHash => Settings.Default.FsContractHash;
        public UInt160 BalanceContractHash => Settings.Default.BalanceContractHash;
        public UInt160 NetmapContractHash => Settings.Default.NetmapContractHash;
        public UInt160 FsIdContractHash => Settings.Default.FsIdContractHash;
        public UInt160 AuditContractHash => Settings.Default.AuditContractHash;
        public UInt160 ReputationContractHash => Settings.Default.AuditContractHash;

        private MorphInvoker morphCli;
        private MainInvoker mainCli;
        public IState state;
        public IActorRef workPool;
        public ProtocolSettings protocolSettings;

        public MainInvoker MainCli { get => mainCli; set => mainCli = value; }
        public MorphInvoker MorphCli { get => morphCli; set => morphCli = value; }
        public IState State { get => state; set => state = value; }
        public IActorRef WorkPool { get => workPool; set => workPool = value; }
        public ProtocolSettings ProtocolSettings { get => protocolSettings; set => protocolSettings = value; }

        public virtual HandlerInfo[] ListenerHandlers()
        {
            return new HandlerInfo[] { };
        }

        public virtual ParserInfo[] ListenerParsers()
        {
            return new ParserInfo[] { };
        }

        public virtual HandlerInfo[] TimersHandlers()
        {
            return new HandlerInfo[] { };
        }
    }
}
