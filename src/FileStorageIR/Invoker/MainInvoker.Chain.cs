using System;
using System.Linq;
using Neo.Cryptography.ECC;
using Neo.FileStorage.Morph.Invoker;

namespace Neo.FileStorage.InnerRing.Invoker
{
    public partial class MainInvoker
    {
        public void InnerRingIndex(ECPoint key, out int index, out int length)
        {
            ECPoint[] innerRing = NeoFSAlphabetList();
            index = KeyPosition(key, innerRing);
            length = innerRing.Length;
        }

        public int AlphabetIndex(ECPoint key)
        {
            return KeyPosition(key, Committee());
        }

        private int KeyPosition(ECPoint key, ECPoint[] list)
        {
            var result = -1;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].Equals(key))
                {
                    result = i;
                    break;
                }
            }
            return result;
        }
    }
}
