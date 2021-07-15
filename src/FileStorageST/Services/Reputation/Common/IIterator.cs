using System;

namespace Neo.FileStorage.Storage.Services.Reputaion.Common
{
    public interface IIterator
    {
        void Iterate(Action<Trust> handler);
    }
}
