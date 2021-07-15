using System;
using Akka.Actor;
using Neo.Cryptography.ECC;
using Neo.FileStorage.InnerRing.Services.Audit;
using Neo.FileStorage.Morph.Invoker;

namespace Neo.FileStorage.InnerRing.Tests
{
    public class TestState : IState
    {
        public IActorRef actor;
        public int innerRingIndex;
        public int innerRingSize;
        public MorphInvoker morphInvoker;
        public string Name = "test";
        public int alphabetIndex;
        public bool isAlphabet;
        public bool isActive;
        public ulong epoch;

        public int AlphabetIndex()
        {
            return alphabetIndex;
        }

        public ulong EpochCounter()
        {
            return epoch;
        }

        public void InitAndVoteForSidechainValidator(ECPoint[] keys)
        {
            throw new NotImplementedException();
        }

        public int InnerRingIndex()
        {
            return innerRingIndex;
        }

        public int InnerRingSize()
        {
            return innerRingSize;
        }

        public bool IsActive()
        {
            return isActive;
        }

        public bool IsAlphabet()
        {
            return isAlphabet;
        }

        public void ResetEpochTimer()
        {
            //actor.Tell(new ResetEvent());
        }

        public void SetEpochCounter(ulong epoch)
        {
            this.epoch = epoch;
        }

        public void VoteForSidechainValidator(ECPoint[] validators)
        {
            Array.Sort(validators);
            var index = InnerRingIndex();
            if (index < 0 || index >= FileStorage.InnerRing.Settings.Default.AlphabetContractHash.Length)
            {
                Utility.Log(Name, LogLevel.Info, "ignore validator vote: node not in alphabet range");
                return;
            }
            if (validators.Length == 0)
            {
                Utility.Log(Name, LogLevel.Info, "ignore validator vote: empty validators list");
                return;
            }
            var epoch = EpochCounter();
            for (int i = 0; i < FileStorage.InnerRing.Settings.Default.AlphabetContractHash.Length; i++)
            {
                try
                {
                    morphInvoker.AlphabetVote(i, epoch, validators);
                }
                catch
                {
                    Utility.Log(Name, LogLevel.Info, string.Format("can't invoke vote method in alphabet contract,alphabet_index:{0},epoch:{1}}", i, epoch));
                }
            }
        }

        public void WriteReport(Report r)
        {
            throw new NotImplementedException();
        }
    }
}
