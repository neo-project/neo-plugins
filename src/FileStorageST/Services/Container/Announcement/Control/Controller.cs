using System.Collections.Generic;
using System.Threading;
using Neo.FileStorage.Storage.Services.Container.Announcement.Route;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Control
{
    public class Controller
    {
        public IIteratorProvider LocalMetrics { get; init; }
        public IWriterProvider LocalAnnouncementTarget { get; init; }
        public IIteratorProvider AnnouncementAccumulator { get; init; }
        public IWriterProvider ResultReceiver { get; init; }
        private readonly Dictionary<ulong, CancellationTokenSource> announceCancellations = new();
        private readonly Dictionary<ulong, CancellationTokenSource> reportCancellations = new();

        public void Start(ulong epoch)
        {
            AnnounceContext context = AcquireAnnouncement(epoch);
            if (context is null) return;
            context.Announce();
            FreeAnnouncement(epoch);
        }

        public void Stop(ulong epoch)
        {
            AnnounceContext context = AcquireReport(epoch);
            if (context is null) return;
            FreeAnnouncement(epoch);
            context.Report();
            FreeReport(epoch);
        }

        public AnnounceContext AcquireAnnouncement(ulong epoch)
        {
            CancellationToken cancellation;
            lock (announceCancellations)
            {
                if (announceCancellations.ContainsKey(epoch))
                {
                    return null;
                }
                CancellationTokenSource source = new();
                announceCancellations[epoch] = source;
                cancellation = source.Token;
            }
            return new AnnounceContext
            {
                Epoch = epoch,
                Controller = this,
                Cancellation = cancellation,
            };
        }

        public AnnounceContext AcquireReport(ulong epoch)
        {
            CancellationToken cancellation;
            lock (reportCancellations)
            {
                if (reportCancellations.ContainsKey(epoch))
                {
                    return null;
                }
                CancellationTokenSource source = new();
                reportCancellations[epoch] = source;
                cancellation = source.Token;
            }
            return new AnnounceContext
            {
                Epoch = epoch,
                Controller = this,
                Cancellation = cancellation,
            };
        }

        public void FreeAnnouncement(ulong epoch)
        {
            lock (announceCancellations)
            {
                if (announceCancellations.TryGetValue(epoch, out CancellationTokenSource token))
                {
                    token.Cancel();
                    announceCancellations.Remove(epoch);
                }
            }
        }

        public void FreeReport(ulong epoch)
        {
            lock (reportCancellations)
            {
                if (reportCancellations.TryGetValue(epoch, out CancellationTokenSource token))
                {
                    token.Cancel();
                    reportCancellations.Remove(epoch);
                }
            }
        }
    }
}
