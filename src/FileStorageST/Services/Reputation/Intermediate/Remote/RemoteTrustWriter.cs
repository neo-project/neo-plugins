using System.Collections.Generic;
using System.Security.Cryptography;
using Google.Protobuf;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.API.Reputation;
using Neo.FileStorage.Storage.Services.Reputaion.Common;
using Neo.FileStorage.Storage.Services.Reputaion.EigenTrust;

namespace Neo.FileStorage.Storage.Services.Reputaion.Intermediate.Remote
{
    public class RemoteTrustWriter : IWriter
    {
        public IterationContext Context { get; init; }
        public ECDsa Key { get; init; }
        public IFSClient Client { get; init; }
        private readonly List<PeerToPeerTrust> buffer = new();

        public void Write(Trust t)
        {
            PeerToPeerTrust trust = new()
            {
                TrustingPeer = new()
                {
                    PublicKey = ByteString.CopyFrom(t.Trusting)
                },
                Trust = new()
                {
                    Peer = new()
                    {
                        PublicKey = ByteString.CopyFrom(t.Peer),
                    },
                    Value = t.Value
                }
            };
            buffer.Add(trust);
        }

        public void Close()
        {
            foreach (var t in buffer)
                Client.AnnounceIntermediateTrust(Context.Epoch, Context.Index, t, new() { Key = Key });
        }
    }
}
