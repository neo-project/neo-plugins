using Neo.FileStorage.InnerRing.Processors;

namespace Neo.FileStorage.InnerRing.Tests.InnerRing.Processors
{
    public class TestActiveState //: IActiveState
    {
        private bool state = false;

        public void SetActive(bool state)
        {
            this.state = state;
        }
        public bool IsActive()
        {
            return state;
        }
    }
}
