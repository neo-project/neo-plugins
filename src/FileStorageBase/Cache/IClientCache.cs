using System;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.Network;

namespace Neo.FileStorage.Cache
{
    public interface IFSClientCache : IDisposable
    {
        IFSClient Get(Address address);
    }
}
