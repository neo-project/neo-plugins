
namespace Neo.Plugins.StateService.Verification
{
    public class Vote
    {
        public int ValidatorIndex;
        public uint RootIndex;
        public byte[] Signature;

        public Vote(uint index, int validator, byte[] signature)
        {
            RootIndex = index;
            ValidatorIndex = validator;
            Signature = signature;
        }
    }
}