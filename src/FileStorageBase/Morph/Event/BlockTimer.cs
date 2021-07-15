using System;
using System.Collections.Generic;

namespace Neo.FileStorage.Morph.Event
{
    public class BlockTimer
    {
        public class DeltaCfg { public bool pulse; }
        private bool rolledBack;
        private Func<uint> dur;
        private uint baseDur;
        private uint mul;
        private uint div;
        private uint cur;
        private uint tgt;
        private Action h;
        private List<BlockTimer> ps;
        private DeltaCfg deltaCfg;

        public BlockTimer(Func<uint> dur, Action h, uint pmul = 1, uint pdiv = 1)
        {
            this.dur = dur;
            this.mul = pmul;
            this.div = pdiv;
            this.h = h;
            this.ps = new List<BlockTimer>();
            this.deltaCfg = new DeltaCfg() { pulse = true };
        }

        public void Delta(uint mul, uint div, Action h, Action<DeltaCfg>[] opts = null)
        {
            var c = new DeltaCfg() { pulse = false };
            if (opts is not null) foreach (var opt in opts) opt(c);
            ps.Add(new BlockTimer(null, h, mul, div));
        }

        public void Reset()
        {
            var d = dur();
            ResetWithBaseInterval(d);
            foreach (var process in ps)
            {
                process.ResetWithBaseInterval(d);
            }

        }

        public void ResetWithBaseInterval(uint d)
        {
            rolledBack = false;
            baseDur = d;
            OnReset();
        }
        private void OnReset()
        {
            var mul = this.mul;
            var div = this.div;
            if (!deltaCfg.pulse && rolledBack && mul < div) mul = div = 1;
            var delta = mul * baseDur / div;
            if (delta == 0) delta = 1;
            tgt = delta;
            cur = 0;
        }

        public void Tick()
        {
            cur++;
            if (cur == tgt)
            {
                h();
                cur = 0;
                rolledBack = true;
                OnReset();
            }
            foreach (var process in ps) process.Tick();
        }

        public static Func<uint> StaticBlockMeter(uint d)
        {
            return () => { return d; };
        }
    }
}
