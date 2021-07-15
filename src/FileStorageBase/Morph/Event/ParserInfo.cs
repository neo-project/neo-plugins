using System;

namespace Neo.FileStorage.Morph.Event
{
    public class ParserInfo
    {
        public ScriptHashWithType ScriptHashWithType;
        public Func<VM.Types.Array, IContractEvent> Parser;
    }
}
