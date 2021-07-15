using System;
using System.Linq;
using Neo.FileStorage.InnerRing.Invoker;
using Neo.FileStorage.Morph.Invoker;

namespace Neo.FileStorage.InnerRing
{
    public class InnerRingIndexer
    {
        private Object lockObject = new();
        private Indexes ind = new();
        private DateTime lastAccess = DateTime.Now;
        private readonly MorphInvoker morphInvoker;
        private readonly TimeSpan timeout;

        public InnerRingIndexer(MorphInvoker invoker, TimeSpan to)
        {
            this.morphInvoker = invoker;
            this.timeout = to;
        }

        public Indexes Update()
        {
            lock (lockObject)
            {
                if (DateTime.Now.Subtract(lastAccess) < timeout) return ind;
                var key = morphInvoker.Wallet.GetAccounts().ToArray()[0].GetKey().PublicKey;
                morphInvoker.InnerRingIndex(key, out int innerRingIndex, out int innerRingSize);
                ind.innerRingIndex = innerRingIndex;
                ind.innerRingSize = innerRingSize;
                ind.alphabetIndex = morphInvoker.AlphabetIndex(key);
                lastAccess = DateTime.Now;
                return ind;
            }
        }

        public int InnerRingIndex()
        {
            Indexes ind = Update();
            return ind.innerRingIndex;
        }

        public int InnerRingSize()
        {
            Indexes ind = Update();
            return ind.innerRingSize;
        }

        public int AlphabetIndex()
        {
            Indexes ind = Update();
            return ind.alphabetIndex;
        }

        public class Indexes
        {
            public int innerRingIndex;
            public int innerRingSize;
            public int alphabetIndex;
        }

    }
}
