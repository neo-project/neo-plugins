using System;
using System.Threading.Tasks;
using Akka.Actor;
using Neo.FileStorage.InnerRing.Invoker;
using Neo.FileStorage.InnerRing.Utils;
using Neo.FileStorage.Morph.Event;
using static Neo.FileStorage.Morph.Event.MorphEvent;
using static Neo.FileStorage.Utils.WorkerPool;

namespace Neo.FileStorage.InnerRing.Processors
{
    public class BalanceContractProcessor : BaseProcessor
    {
        public override string Name => "BalanceContractProcessor";
        private const string LockNotification = "Lock";
        public Fixed8ConverterUtil Convert;

        public override HandlerInfo[] ListenerHandlers()
        {
            ScriptHashWithType scriptHashWithType = new ScriptHashWithType()
            {
                Type = LockNotification,
                ScriptHashValue = BalanceContractHash
            };
            HandlerInfo handler = new HandlerInfo()
            {
                ScriptHashWithType = scriptHashWithType,
                Handler = HandleLock
            };
            return new HandlerInfo[] { handler };
        }

        public override ParserInfo[] ListenerParsers()
        {
            ScriptHashWithType scriptHashWithType = new ScriptHashWithType()
            {
                Type = LockNotification,
                ScriptHashValue = BalanceContractHash
            };
            ParserInfo parser = new ParserInfo()
            {
                ScriptHashWithType = scriptHashWithType,
                Parser = LockEvent.ParseLockEvent,
            };
            return new ParserInfo[] { parser };
        }

        public void HandleLock(IContractEvent morphEvent)
        {
            LockEvent lockEvent = (LockEvent)morphEvent;
            Utility.Log(Name, LogLevel.Info, string.Format("notification:type:lock,value:{0}", lockEvent.Id.ToHexString()));
            WorkPool.Tell(new NewTask() { Process = Name, Task = new Task(() => ProcessLock(lockEvent)) });
        }

        public void ProcessLock(LockEvent lockEvent)
        {
            if (!State.IsAlphabet())
            {
                Utility.Log(Name, LogLevel.Info, "non alphabet mode, ignore balance lock");
                return;
            }
            try
            {
                MainCli.CashOutCheque(lockEvent.Id, Convert.ToFixed8(lockEvent.Amount), lockEvent.UserAccount, lockEvent.LockAccount);
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Error, string.Format("can't send lock asset tx:{0}", e.Message));
            }
        }
    }
}
