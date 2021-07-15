using System;
using System.Collections.Generic;
using Neo.FileStorage.Morph.Event;

namespace Neo.FileStorage.Storage.Processors
{
    public class ContainerProcessor : IProcessor
    {
        public const string StartEstimationNotifyEvent = "StartEstimation";
        public const string StopEstimationNotifyEvent = "StopEstimation";
        private readonly UInt160 ContainerScriptHash = Settings.Default.ContainerContractHash;
        private readonly List<ParserInfo> parsers = new();
        private readonly List<HandlerInfo> handlers = new();

        ParserInfo[] IProcessor.ListenerParsers()
        {
            return parsers.ToArray();
        }

        HandlerInfo[] IProcessor.ListenerHandlers()
        {
            return handlers.ToArray();
        }

        HandlerInfo[] IProcessor.TimersHandlers()
        {
            return Array.Empty<HandlerInfo>();
        }

        public void AddStartEstimateContainerParser(Func<VM.Types.Array, IContractEvent> parser)
        {
            parsers.Add(new()
            {
                ScriptHashWithType = new()
                {
                    ScriptHashValue = ContainerScriptHash,
                    Type = StartEstimationNotifyEvent,
                },
                Parser = parser
            });
        }

        public void AddStopEstimateContainerParser(Func<VM.Types.Array, IContractEvent> parser)
        {
            parsers.Add(new()
            {
                ScriptHashWithType = new()
                {
                    ScriptHashValue = ContainerScriptHash,
                    Type = StopEstimationNotifyEvent,
                },
                Parser = parser
            });
        }

        public void AddStartEstimateHandler(Action<IContractEvent> handler)
        {
            handlers.Add(new()
            {
                ScriptHashWithType = new()
                {
                    ScriptHashValue = ContainerScriptHash,
                    Type = StartEstimationNotifyEvent,
                },
                Handler = handler,
            });
        }

        public void AddStopEstimateHandler(Action<IContractEvent> handler)
        {
            handlers.Add(new()
            {
                ScriptHashWithType = new()
                {
                    ScriptHashValue = ContainerScriptHash,
                    Type = StopEstimationNotifyEvent,
                },
                Handler = handler,
            });
        }
    }
}
