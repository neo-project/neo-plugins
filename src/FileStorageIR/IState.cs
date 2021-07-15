using Neo.Cryptography.ECC;
using Neo.FileStorage.InnerRing.Services.Audit;

namespace Neo.FileStorage.InnerRing
{
    public interface IState : IActiveState, IEpochState, IAlphabetState, IIndexer, IVoter, IEpochTimerReseter, IReporter { }

    public interface IAlphabetState
    {
        bool IsAlphabet();

        int AlphabetIndex();
    }

    public interface IActiveState
    {
        bool IsActive();
    }

    public interface IEpochState
    {
        void SetEpochCounter(ulong epoch);
        ulong EpochCounter();
    }

    public interface IIndexer
    {
        int InnerRingIndex();
        int InnerRingSize();
    }

    public interface IVoter
    {
        void InitAndVoteForSidechainValidator(ECPoint[] keys);
        void VoteForSidechainValidator(ECPoint[] keys);
    }

    public interface IEpochTimerReseter
    {
        void ResetEpochTimer();
    }
}
