using Neo.Network.P2P;
using System.Net;
using System.Net.NetworkInformation;

namespace Neo.Plugins
{
    public class NodePingReply
    {
        private PingReply Reply;
        private IPEndPoint Address;
        public string AddressAndPort => Address.ToString();
        public IPStatus Status => GetStatus();
        public long RoundtripTime => GetRoundtripTime();
        public bool isConnectedNode { get; private set; }
        public uint LastBlockIndex { get; private set; } = 0;

        public NodePingReply(RemoteNode node, PingReply reply)
        {
            this.Address = node.Remote;
            this.LastBlockIndex = node.LastBlockIndex;
            this.isConnectedNode = true;
            this.Reply = reply;
        }

        public NodePingReply(IPEndPoint ipEndPoint, PingReply reply)
        {
            this.Address = ipEndPoint;
            this.isConnectedNode = false;
            this.Reply = reply;
        }

        public string GetNodeInfo()
        {
            if (isConnectedNode)
            {
                return $"height: {LastBlockIndex}";
            }
            else
            {
                return "unconnected";
            }
        }

        private IPStatus GetStatus()
        {
            if (Reply != null)
            {
                return Reply.Status;
            }
            return default;
        }

        private long GetRoundtripTime()
        {
            if (Reply != null)
            {
                return Reply.RoundtripTime;
            }
            return 0;
        }
    }
}
