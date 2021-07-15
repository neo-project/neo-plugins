using System;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Placement;
using Neo.FileStorage.Storage.Services.Object.Util;
using FSAddress = Neo.FileStorage.API.Refs.Address;

namespace Neo.FileStorage.Storage.Services.Object.Util
{
    public class TraverserGenerator : ITraverserGenerator
    {
        private readonly MorphInvoker morphInvoker;
        private readonly Network.Address localAddress;
        private readonly int successAfter;
        private readonly bool trackCopies;

        public TraverserGenerator(MorphInvoker invoker, Network.Address address, int successAfter = 0, bool trackCopies = true)
        {
            morphInvoker = invoker;
            localAddress = address;
            this.successAfter = successAfter;
            this.trackCopies = trackCopies;
        }

        public Traverser GenerateTraverser(FSAddress address, ulong epoch)
        {
            var nm = morphInvoker.EpochSnapshot(epoch);
            if (nm is null)
            {
                throw new Exception(nameof(TraverserGenerator) + " could not get latest netmap");
            }
            var container = morphInvoker.GetContainer(address.ContainerId)?.Container;
            if (container is null)
            {
                throw new Exception(nameof(TraverserGenerator) + " could not get container");
            }
            var builder = new RemotePlacementBuilder(morphInvoker, localAddress);
            return new Traverser(builder, container.PlacementPolicy, address, successAfter, trackCopies);
        }
    }
}
