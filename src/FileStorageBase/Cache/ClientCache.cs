using System;
using System.Collections.Concurrent;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.Network;

namespace Neo.FileStorage.Cache
{
    public class ClientCache : IFSClientCache
    {
        private readonly ConcurrentDictionary<string, Client> clients = new();

        public virtual IFSClient Get(Address address)
        {
            var m_addr = address.ToString();
            if (clients.TryGetValue(m_addr, out Client client))
            {
                return client;
            }
            //TODO: tls
            var host = "http://" + address.ToHostAddressString();
            client = new Client(null, host);
            clients[m_addr] = client;
            return client;
        }

        public virtual void Dispose()
        {
            foreach (var client in clients.Values)
            {
                client.Dispose();
            }
            clients.Clear();
        }
    }
}
