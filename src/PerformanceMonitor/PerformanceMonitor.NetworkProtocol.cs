using Neo.ConsoleService;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.RPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins
{
    partial class PerformanceMonitor
    {
        /// <summary>
        /// Prints the time passed in seconds since the last block
        /// </summary>
        [ConsoleCommand("block timesincelast", Category = "Block Commands", Description = "Show the time passed in seconds since the last block.")]
        private void OnBlockTimeSinceLastCommand()
        {
            var timeSinceLastBlockInSec = GetTimeSinceLastBlock() / 1000;
            Console.WriteLine($"Time since last block: {timeSinceLastBlockInSec} seconds");
        }

        /// <summary>
        /// Gets the time passed in seconds since the last block
        /// </summary>
        /// <returns>
        /// Returns the time since the last block was persisted in milliseconds
        /// </returns>
        [RpcMethod]
        public JObject GetTimeSinceLastBlock(JArray _params)
        {
            if (_params.Count != 0)
            {
                throw new RpcException(-32602, "Invalid params");
            }
            return GetTimeSinceLastBlock();
        }

        /// <summary>
        /// Calculates the time passed since the last block
        /// </summary>
        /// <returns>
        /// Returns the time since the last block was persisted in milliseconds
        /// </returns>
        private ulong GetTimeSinceLastBlock()
        {
            var index = Blockchain.Singleton.Height;
            var block = Blockchain.Singleton.GetBlock(index);

            var currentTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return currentTimestamp - block.Timestamp;
        }

        /// <summary>
        /// Prints the number of nodes connected to the local node
        /// </summary>
        [ConsoleCommand("connected", Category = "Network Commands", Description = "Show the number of nodes connected to the local node.")]
        private void OnConnectedNodesCommand()
        {
            var nodes = GetConnectedNodes();

            Console.WriteLine($"Connected nodes: {nodes.Count}");

            foreach (var node in nodes)
            {
                var remoteAddressAndPort = $"{node.Remote.Address}:{node.Remote.Port}";
                Console.WriteLine($"  ip: {remoteAddressAndPort,-25}\theight: {node.LastBlockIndex,-8}");
            }
        }

        /// <summary>
        /// Prints the number of nodes connected to the local node
        /// </summary>
        /// <returns>
        /// Returns a list with the connected nodes
        /// </returns>
        [RpcMethod]
        public JObject GetConnectedNodes(JArray _params)
        {
            if (_params.Count != 0)
            {
                throw new RpcException(-32602, "Invalid params");
            }

            var connectedNodes = GetConnectedNodes();
            var nodes = new JArray();
            foreach (var remoteNode in connectedNodes)
            {
                var node = new JObject();
                node["address"] = $"{remoteNode.Remote.Address}:{remoteNode.Remote.Port}";
                node["lastblockindex"] = remoteNode.LastBlockIndex;
                nodes.Add(node);
            }

            return nodes;
        }

        /// <summary>
        /// Get a list of the nodes connected to the local node
        /// </summary>
        /// <returns>
        /// Returns a list with the connected nodes if the <see cref="LocalNode.GetRemoteNodes"/>
        /// is a implementation of <see cref="ICollection{RemoteNode}"/>; otherwise,
        /// returns an empty list
        /// </returns>
        private ICollection<RemoteNode> GetConnectedNodes()
        {
            UpdateRemotesHeight();
            var remotes = LocalNode.Singleton.GetRemoteNodes();
            if (remotes is ICollection<RemoteNode>)
            {
                return LocalNode.Singleton.GetRemoteNodes() as ICollection<RemoteNode>;
            }

            return new List<RemoteNode>();
        }

        /// <summary>
        /// Updates the remote nodes block height
        /// </summary>
        private void UpdateRemotesHeight()
        {
            var remotesHeightUpdate = new TaskCompletionSource<bool>();

            P2PMessageHandler p2pMessage = (message) =>
            {
                remotesHeightUpdate.TrySetResult(true);
            };

            OnP2PMessageEvent += p2pMessage;
            // updates the height of the remote nodes
            SendBlockchainPingMessage();

            // waits the ping message response to continue
            remotesHeightUpdate.Task.Wait(1000);
            OnP2PMessageEvent -= p2pMessage;
        }

        /// <summary>
        /// Send a ping message to the node specified by its IP address.
        /// If none is specified, send a ping message to each peer connected to the local node
        /// </summary>
        [ConsoleCommand("ping", Category = "Network Commands",
            Description = "Send a ping message to the node specified by its IP address.\n" +
                          "If none is specified, send a ping message to each peer connected to the local node")]
        private void OnPingPeersCommand(string ipaddress = null)
        {
            UpdateRemotesHeight();
            if (ipaddress != null)
            {
                if (!IPAddress.TryParse(ipaddress, out var address))
                {
                    Console.WriteLine("Invalid parameter");
                    return;
                }

                PingRemoteNode(address, true);
            }
            else
            {
                PingAll(true);
            }
        }

        /// <summary>
        /// Send a ping message to each peer connected to the local node
        /// </summary>
        /// <returns>
        /// Returns a list with the ping replies of each peer
        /// </returns>
        [RpcMethod]
        public JObject Ping(JArray _params)
        {
            if (_params.Count != 0)
            {
                throw new RpcException(-32602, "Invalid params");
            }

            var replies = PingAll();
            var result = new JArray();

            foreach (var reply in replies)
            {
                var node = new JObject();
                node["address"] = reply.AddressAndPort;
                node["connected"] = reply.isConnectedNode;
                if (reply.isConnectedNode)
                {
                    node["lastblockindex"] = reply.LastBlockIndex;
                }

                var pingReply = new JObject();
                pingReply["status"] = reply.Status;
                if (reply.Status == IPStatus.Success)
                {
                    pingReply["roundtriptime"] = reply.RoundtripTime;
                }

                var nodeReply = new JObject();
                nodeReply["node"] = node;
                nodeReply["pingreply"] = pingReply;
                result.Add(nodeReply);
            }
            return result;
        }

        /// <summary>
        /// Sends a ping message to the IP addresses of all connected and unconnected peers
        /// </summary>
        /// <param name="printMessages">
        /// Specifies if the messages should be printed in the console.
        /// </param>
        /// <returns>
        /// Returns a list with the ping replies of each peer
        /// </returns>
        private List<NodePingReply> PingAll(bool printMessages = false)
        {
            List<NodePingReply> replies = new List<NodePingReply>();

            var tasks = new List<Task>();
            if (printMessages)
            {
                Console.WriteLine($"Connected nodes: {LocalNode.Singleton.ConnectedCount}\tUnconnected nodes: {LocalNode.Singleton.UnconnectedCount}");
            }

            // ping remote nodes
            foreach (RemoteNode node in LocalNode.Singleton.GetRemoteNodes())
            {
                Task ping = new Task(() =>
                {
                    var reply = TryPing(node.Remote.Address);
                    var nodeReply = new NodePingReply(node, reply);
                    replies.Add(nodeReply);

                    if (printMessages && reply != null)
                    {
                        PrintPingReply(nodeReply);
                    }
                });
                tasks.Add(ping);
                ping.Start();
            }

            // ping unconnected peers
            foreach (var node in LocalNode.Singleton.GetUnconnectedPeers())
            {
                Task ping = new Task(() =>
                {
                    var reply = TryPing(node.Address);
                    var nodeReply = new NodePingReply(node, reply);
                    replies.Add(nodeReply);

                    if (printMessages && reply != null)
                    {
                        PrintPingReply(nodeReply);
                    }
                });
                tasks.Add(ping);
                ping.Start();
            }

            Task.WaitAll(tasks.ToArray());
            // return all ping replies
            return replies;
        }

        /// <summary>
        /// Sends a ping message to specified IP address if it is the address of a remote node
        /// </summary>
        /// <param name="printMessages">
        /// Specifies if the messages should be printed in the console.
        /// </param>
        /// <returns>
        /// Returns a list with the ping reply of each peer
        /// </returns>
        private List<NodePingReply> PingRemoteNode(IPAddress ipaddress, bool printMessages = false)
        {
            List<NodePingReply> replies = new List<NodePingReply>();

            var remoteNodes = GetRemoteNode(ipaddress);
            if (remoteNodes == null || remoteNodes.Count == 0)
            {
                if (printMessages)
                {
                    Console.WriteLine("Input address was not a connected peer");
                }
                return replies;
            }

            var reply = TryPing(ipaddress);
            foreach (var node in remoteNodes)
            {
                var nodeReply = new NodePingReply(node, reply);
                replies.Add(nodeReply);
                if (printMessages)
                {
                    PrintPingReply(nodeReply);
                }
            }

            return replies;
        }

        /// <summary>
        /// Prints the information of the ping reply sent to a peer
        /// </summary>
        /// <param name="nodeReply">
        /// The object with the information about the node and the ping reply
        /// </param>
        private void PrintPingReply(NodePingReply nodeReply)
        {
            if (nodeReply != null)
            {
                if (nodeReply.Status == IPStatus.Success)
                {
                    Console.WriteLine(
                        $"  {nodeReply.AddressAndPort,-25}" +
                        $"\t{nodeReply.GetNodeInfo(),-16}" +
                        $"\t{nodeReply.RoundtripTime,-30:###0 ms}");
                }
                else
                {
                    Console.WriteLine(
                        $"  {nodeReply.AddressAndPort,-25}" +
                        $"\t{nodeReply.GetNodeInfo(),-16}" +
                        $"\t{nodeReply.Status,-30}");
                }
            }
        }

        /// <summary>
        /// Verifies if a given IP address is the address of a remote node.
        /// </summary>
        /// <param name="ipaddress">
        /// The IP address to be verified.
        /// </param>
        /// <returns>
        /// If the <paramref name="ipaddress"/> is the address of a remote node, returns a list
        /// of the remote nodes with that address; otherwise, returns an empty list.
        /// </returns>
        private List<RemoteNode> GetRemoteNode(IPAddress ipaddress)
        {
            List<RemoteNode> nodes = new List<RemoteNode>();
            foreach (RemoteNode node in LocalNode.Singleton.GetRemoteNodes())
            {
                if (node.Remote.Address.Equals(ipaddress))
                {
                    nodes.Add(node);
                }
            }

            return nodes;
        }

        /// <summary>
        /// Tries to send a ping message to the specified IP address.
        /// </summary>
        /// <param name="ipaddress">
        /// The IP address to send the ping message.
        /// </param>
        /// <returns>
        /// Returns null if an exception is thrown; otherwise returns the <see cref="PingReply"/>
        /// object that is the response of ping
        /// </returns>
        private PingReply TryPing(IPAddress ipaddress)
        {
            try
            {
                int timeoutInMs = 10000; // ping timeout is 10 seconds

                Ping ping = new Ping();
                var reply = ping.Send(ipaddress, timeoutInMs);

                return reply;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Process "rpc time" command
        /// Prints the time in milliseconds to receive the response of a rpc request
        /// </summary>
        /// <param name="url">
        /// The url of the rpc server
        /// </param>
        [ConsoleCommand("rpc time", Category = "Network Commands", Description = "Show the time in milliseconds to receive the response of a rpc request.")]
        private void OnRpcTimeCommand(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                Console.WriteLine("Input url is invalid");
                return;
            }

            try
            {
                var responseTime = GetRpcResponseTime(url);

                if (responseTime > 0)
                {
                    Console.WriteLine($"RPC response time: {responseTime:0.##} milliseconds");
                }
            }
            catch (FileNotFoundException)
            {
                // for this command it is required that the RpcClient plugin is installed
                Console.WriteLine("Install RpcClient module to use the rpc time command.");
            }
        }

        /// <summary>
        /// Gets the time in milliseconds to receive the response of a rpc request
        /// </summary>
        /// <returns>
        /// Returns the time of the response from the rpc request in milliseconds
        /// </returns>
        [RpcMethod]
        public JObject GetRpcTime(JArray _params)
        {
            if (_params.Count != 1)
            {
                throw new RpcException(-32602, "Invalid params");
            }
            var url = _params[0].AsString();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                throw new RpcException(-32602, "Invalid params");
            }

            try
            {
                var responseTime = GetRpcResponseTime(url);
                if (responseTime <= 0)
                {
                    throw new RpcException(-32602, "Invalid params");
                }

                return responseTime;
            }
            catch (FileNotFoundException)
            {
                // for this command it is required that the RpcClient plugin is installed
                throw new RpcException(-32500, "Application error");
            }
        }

        /// <summary>
        /// Gets the time to receive the response of a rpc request
        /// </summary>
        /// <param name="url">
        /// The url of the rpc server
        /// </param>
        /// <returns>
        /// Returns zero if any exception is thrown; otherwise, returns the time in milliseconds of
        /// the response from the rpc request
        /// </returns>
        private long GetRpcResponseTime(string url, bool printMessages = false)
        {
            if (printMessages)
            {
                Console.WriteLine($"Sending a RPC request to '{url}'...");
            }
            bool hasThrownException = true;

            RpcClient client = new RpcClient(url);
            Stopwatch watch = Stopwatch.StartNew();

            try
            {
                var success = Task.Run(() =>
                {
                    client.GetBlockCount();
                    hasThrownException = false;
                }).Wait(30 * 1000);  // set timeout to 30 seconds

                if (!success && printMessages)
                {
                    hasThrownException = true;
                    Console.WriteLine("Timeout");
                }
            }
            catch (HttpRequestException)
            {
                if (printMessages)
                {
                    Console.WriteLine("Input url is not a the url of a valid RPC server");
                }
            }
            catch (Exception e)
            {
                if (printMessages)
                {
                    Console.WriteLine(
                        "An exception was thrown while trying to send the RPC request:\n" +
                        $"\t{e.GetType()}\n" +
                        $"\t{e.Message}");
                }
            }

            watch.Stop();

            if (hasThrownException)
            {
                return 0;
            }

            return watch.ElapsedMilliseconds;
        }
    }
}
