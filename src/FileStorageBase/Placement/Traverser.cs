using System;
using System.Collections.Generic;
using System.Linq;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.Placement
{
    public class Traverser
    {
        private readonly List<List<Node>> vectors;
        private readonly List<int> rem;

        public Traverser(IPlacementBuilder builder, PlacementPolicy policy, Address address, int successAfter = 0, bool trackCopies = true)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));
            else if (policy is null)
                throw new ArgumentNullException(nameof(policy));
            else if (address is null)
                throw new ArgumentNullException(nameof(address));
            var ns = builder.BuildPlacement(address, policy);
            if (successAfter != 0)
            {
                ns = new List<List<Node>> { ns.Flatten() };
                rem = new List<int> { successAfter };
            }
            else
            {
                rem = policy.Replicas.Select(p => trackCopies ? (int)p.Count : -1).ToList();
            }
            vectors = ns;
        }

        public List<Network.Address> Next()
        {
            SkipEmptyVectors();
            if (!vectors.Any())
                return new();
            else if (vectors[0].Count < rem[0])
                return new();

            var count = rem[0];
            if (count < 0)
                count = vectors[0].Count;

            List<Network.Address> addrs = new();

            for (int i = 0; i < count; i++)
            {
                Network.Address addr;
                try
                {
                    addr = Network.Address.FromString(vectors[0][i].NetworkAddress);
                }
                catch (Exception)
                {
                    return new();
                }
                addrs.Add(addr);
            }

            vectors[0] = vectors[0].Skip(count).ToList();
            return addrs;
        }

        private void SkipEmptyVectors()
        {
            for (int i = 0; i < vectors.Count; i++)
            {
                if (vectors[i].Count == 0 && rem[i] <= 0 || rem[0] == 0)
                {
                    vectors.RemoveAt(i);
                    rem.RemoveAt(i);
                    i--;
                }
                else
                    break;
            }
        }

        public void SubmitSuccess()
        {
            if (rem.Count > 0)
                rem[0]--;
        }

        public bool Success()
        {
            return !rem.Any(p => 0 < p);
        }
    }
}
