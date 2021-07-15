using System;
using Neo.FileStorage.InnerRing.Processors;
using Neo.FileStorage.Morph.Event;
using Neo.FileStorage.Morph.Invoker;
using static Neo.FileStorage.InnerRing.Events.MorphEvent;
using static Neo.FileStorage.InnerRing.Timer.TimerTickEvent;

namespace Neo.FileStorage.InnerRing.Timer
{
    public class Helper
    {
        public static BlockTimer NewEpochTimer(EpochTimerArgs args)
        {
            BlockTimer epochTimer = new BlockTimer(BlockTimer.StaticBlockMeter(Settings.Default.EpochDuration), () => { args.processor.HandleNewEpochTick(new NewEpochTickEvent()); });
            epochTimer.Delta(
                args.stopEstimationDMul,
                args.stopEstimationDDiv,
                () =>
                {
                    long epochN = (long)args.epoch.EpochCounter();
                    if (epochN == 0) return;
                    args.client.StopEstimation(epochN - 1);
                });
            epochTimer.Delta(
                args.collectBasicIncome.durationMul,
                args.collectBasicIncome.durationDiv,
                () =>
                {
                    ulong epochN = args.epoch.EpochCounter();
                    if (epochN == 0) return;
                    args.collectBasicIncome.handler(new BasicIncomeCollectEvent() { epoch = epochN - 1 });
                });
            epochTimer.Delta(
                args.distributeBasicIncome.durationMul,
                args.distributeBasicIncome.durationDiv,
                () =>
                {
                    ulong epochN = args.epoch.EpochCounter();
                    if (epochN == 0) return;
                    args.distributeBasicIncome.handler(new BasicIncomeDistributeEvent() { epoch = epochN - 1 });
                });
            return epochTimer;
        }

        public static BlockTimer NewEmissionTimer(EmitTimerArgs args)
        {
            return new BlockTimer(BlockTimer.StaticBlockMeter(Settings.Default.AlphabetDuration), () => { args.processor.HandleGasEmission(new NewAlphabetEmitTickEvent()); });
        }

        public class EmitTimerArgs
        {
            public NeoSystem context;
            public AlphabetContractProcessor processor;
            public uint epochDuration;
        }

        public class EpochTimerArgs
        {
            public NeoSystem context;
            public NetMapContractProcessor processor;
            public MorphInvoker client;
            public IState epoch;
            public uint epochDuration;
            public uint stopEstimationDMul;
            public uint stopEstimationDDiv;
            public SubEpochEventHandler collectBasicIncome;
            public SubEpochEventHandler distributeBasicIncome;
        }

        public class SubEpochEventHandler
        {
            public Action<IContractEvent> handler;
            public uint durationMul;
            public uint durationDiv;
        }
    }
}
