using System;

namespace Neo.FileStorage.InnerRing.Utils
{

    public class Once
    {
        private bool flag;
        private readonly object obj = new();

        public void Do(Action action)
        {
            if (!flag)
            {
                lock (obj)
                {
                    if (!flag)
                    {
                        flag = true;
                        action();
                    }
                }
            }
        }
    }
}
