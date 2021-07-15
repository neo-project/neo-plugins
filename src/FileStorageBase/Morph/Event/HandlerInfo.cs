using System;

namespace Neo.FileStorage.Morph.Event
{
    public class HandlerInfo
    {
        public ScriptHashWithType ScriptHashWithType;
        public Action<IContractEvent> Handler;
    }
}
