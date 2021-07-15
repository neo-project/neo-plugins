using Neo.FileStorage.Storage.Services.Container.Announcement.Route;
using System;
using System.Threading;

namespace Neo.FileStorage.Storage.Services.Container.Announcement
{
    public class SimpleProvider : IWriterProvider, IIteratorProvider
    {
        private readonly IWriter writer;
        private readonly IIterator iterator;

        public SimpleProvider(IWriter w)
        {
            writer = w;
        }

        public SimpleProvider(IIterator i)
        {
            iterator = i;
        }

        public IWriter InitWriter(CancellationToken cancellation)
        {
            if (writer is null) throw new InvalidOperationException($"{nameof(SimpleProvider)} no writer");
            return writer;
        }

        public IIterator InitIterator(CancellationToken cancellation)
        {
            if (iterator is null) throw new InvalidOperationException($"{nameof(SimpleProvider)} no iterator");
            return iterator;
        }
    }
}
