using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Multiformats.Address;
using Multiformats.Address.Net;

namespace Neo.FileStorage.Network
{
    public class Address : IEquatable<Address>
    {
        public const string L4Protocol = "tcp";

        private Multiaddress ma;

        public Address() { }

        public Address(Multiaddress m)
        {
            ma = m;
        }

        public Address Encapsulate(Address other)
        {
            return new(ma.Encapsulate(other.ma));
        }

        public Address Decapsulate(Address other)
        {
            return new(ma.Decapsulate(other.ma));
        }

        public string ToIPAddressString()
        {
            return ma.ToEndPoint().ToString();
        }

        public string ToHostAddressString()
        {
            return DialArgs(ma);
        }

        public static Address FromString(string s)
        {
            Multiaddress m;
            try
            {
                m = Multiaddress.Decode(s);
            }
            catch (Exception)
            {
                m = Multiaddress.Decode(MultiAddrStringFromHostAddr(s));
            }
            return new Address(m);
        }

        bool IEquatable<Address>.Equals(Address other)
        {
            if (other is null) return false;
            return other.ma.Equals(ma);
        }

        /// <summary>
        /// multiaddrStringFromHostAddr converts "localhost:8080" to "/dns4/localhost/tcp/8080"
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private static string MultiAddrStringFromHostAddr(string host)
        {
            if (0 < host.Length && host[0] == ':') host = "0.0.0.0" + host;
            if (host.Last() == ':') host += "0";
            var endPoint = IPEndPoint.Parse(host);
            var addr = endPoint.Address;
            var port = endPoint.Port;
            var prefix = "/dns4";
            var s = addr.ToString();
            if (addr.AddressFamily == AddressFamily.InterNetwork)
                prefix = "/ip4";
            else if (addr.AddressFamily == AddressFamily.InterNetworkV6)
                prefix = "/ip6";
            return string.Join('/', prefix, s, L4Protocol, port);
        }

        /// <summary>
        /// IPAddrFromMultiaddr converts "/dns4/localhost/tcp/8080" to "192.168.0.1:8080".
        /// </summary>
        /// <param name="multiaddr"></param>
        /// <returns></returns>
        public static string IPAddrFromMultiaddr(string multiaddr)
        {
            return FromString(multiaddr).ToIPAddressString();
        }

        public override string ToString()
        {
            return ma.ToString();
        }

        private string DialArgs(Multiaddress ma)
        {
            var res = DialArgsComponents(ma);
            if (res.HostName)
            {
                return res.Network switch
                {
                    "ip" or "ip4" or "ip6" => res.IP,
                    "tcp" or "tcp4" or "tcp6" or "udp" or "udp4" or "udp6" => res.IP + ":" + res.Port,
                    _ => throw new InvalidOperationException("unreachable"),
                };
            }
            return res.Network switch
            {
                "ip4" => res.IP,
                "tcp4" or "udp4" => res.IP + ":" + res.Port,
                "tcp6" or "udp6" => "[" + res.IP + "]" + ":" + res.Port,
                "unix" => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? res.IP.Replace("/", "\\") : res.IP,
                _ => throw new InvalidOperationException($"{ma} is not a 'thin waist' address"),
            };
        }

        private class DialArgsComponentsResult
        {
            public string Network;
            public string IP;
            public string Port;
            public bool HostName;
        }

        private DialArgsComponentsResult DialArgsComponents(Multiaddress ma)
        {
            DialArgsComponentsResult res = new();
            foreach (var p in ma.Protocols)
            {
                switch (res.Network)
                {
                    case null:
                        {
                            switch (p)
                            {
                                case Multiformats.Address.Protocols.IP4 ip4:
                                    {
                                        res.Network = "ip4";
                                        res.IP = ip4.Value.ToString();
                                        continue;
                                    }
                                case Multiformats.Address.Protocols.IP6 ip6:
                                    {
                                        res.Network = "ip6";
                                        res.IP = ip6.Value.ToString();
                                        continue;
                                    }
                                case Multiformats.Address.Protocols.DNS dns:
                                    {
                                        res.Network = "ip";
                                        res.IP = dns.Value.ToString();
                                        res.HostName = true;
                                        continue;
                                    }
                                case Multiformats.Address.Protocols.DNS4 dns4:
                                    {
                                        res.Network = "ip4";
                                        res.IP = dns4.Value.ToString();
                                        res.HostName = true;
                                        continue;
                                    }
                                case Multiformats.Address.Protocols.DNS6 dns6:
                                    {
                                        res.Network = "ip6";
                                        res.IP = dns6.Value.ToString();
                                        res.HostName = true;
                                        continue;
                                    }
                                case Multiformats.Address.Protocols.Unix unix:
                                    {
                                        res.Network = "unix";
                                        res.IP = unix.Value.ToString();
                                        return res;
                                    }
                            }
                            break;
                        }
                    case "ip":
                        {
                            switch (p)
                            {
                                case Multiformats.Address.Protocols.UDP:
                                    res.Network = "udp";
                                    break;
                                case Multiformats.Address.Protocols.TCP:
                                    res.Network = "tcp";
                                    break;
                                default:
                                    return res;
                            }
                            res.Port = p.Value.ToString();
                            break;
                        }
                    case "ip4":
                        {
                            switch (p)
                            {
                                case Multiformats.Address.Protocols.UDP:
                                    res.Network = "udp4";
                                    break;
                                case Multiformats.Address.Protocols.TCP:
                                    res.Network = "tcp4";
                                    break;
                                default:
                                    return res;
                            }
                            res.Port = p.Value.ToString();
                            break;
                        }
                    case "ip6":
                        {
                            switch (p)
                            {
                                case Multiformats.Address.Protocols.UDP:
                                    res.Network = "udp6";
                                    break;
                                case Multiformats.Address.Protocols.TCP:
                                    res.Network = "tcp6";
                                    break;
                                default:
                                    return res;
                            }
                            res.Port = p.Value.ToString();
                            break;
                        }
                }
            }
            return res;
        }
    }
}
