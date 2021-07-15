using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.Storage.Services.Container.Announcement.Control;
using Neo.FileStorage.Storage.Services.Container.Announcement.Route;
using static Neo.FileStorage.Storage.Tests.Helper;
using FSAnnouncement = Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types.Announcement;

namespace Neo.FileStorage.Tests.Services.Container.Announcement
{
    [TestClass]
    public class UT_Controller
    {
        private class TestAnnouncementStorage : IIteratorProvider, IWriterProvider, IIterator, IWriter
        {
            private readonly ConcurrentDictionary<ulong, List<FSAnnouncement>> storage = new();
            public IIterator I { get; init; }
            public IWriter W { get; init; }
            public int Count => storage.Values.Aggregate((a, b) => a.Concat(b).ToList()).Count;

            public IIterator InitIterator(CancellationToken cancellation)
            {
                if (I is not null) return I;
                return this;
            }

            public void Iterate(Func<FSAnnouncement, bool> filter, Action<FSAnnouncement> handler)
            {
                foreach (var ans in storage.Values)
                {
                    foreach (var an in ans)
                    {
                        if (filter(an))
                            handler(an);
                    }
                }
            }

            public IWriter InitWriter(CancellationToken cancellation)
            {
                if (W is not null) return W;
                return this;
            }

            public void Put(FSAnnouncement announcement)
            {
                if (!storage.TryGetValue(announcement.Epoch, out var value))
                {
                    value = new();
                    storage[announcement.Epoch] = value;
                }
                value.Add(announcement);
            }

            public void Close() { }
        }

        [TestMethod]
        public void TestSimpleScenario()
        {
            var resultStorage = new TestAnnouncementStorage();
            var accumulatingStorageN2 = new TestAnnouncementStorage();
            var localStorageN1 = new TestAnnouncementStorage();
            var localStorageN2 = new TestAnnouncementStorage();
            var ctrlN1 = new Controller
            {
                LocalMetrics = localStorageN1,
                AnnouncementAccumulator = new TestAnnouncementStorage(),
                LocalAnnouncementTarget = new TestAnnouncementStorage
                {
                    W = accumulatingStorageN2,
                },
                ResultReceiver = resultStorage,
            };
            var ctrlN2 = new Controller
            {
                LocalMetrics = localStorageN2,
                AnnouncementAccumulator = accumulatingStorageN2,
                LocalAnnouncementTarget = new TestAnnouncementStorage
                {
                    W = resultStorage,
                },
                ResultReceiver = resultStorage,
            };

            const ulong processEpoch = 10;
            const int goodNum = 4;
            List<FSAnnouncement> announces = new();
            for (int i = 0; i < goodNum; i++)
            {
                var an = RandomAnnouncement();
                an.Epoch = processEpoch;
                announces.Add(an);
            }
            for (int i = 0; i < goodNum / 2; i++)
            {
                localStorageN1.Put(announces[i]);
            }
            for (int i = goodNum / 2; i < goodNum; i++)
                localStorageN2.Put(announces[i]);
            Task[] tasks = new Task[2];
            tasks[0] = Task.Run(() =>
            {
                ctrlN1.Start(processEpoch);
            });
            tasks[1] = Task.Run(() =>
            {
                ctrlN2.Start(processEpoch);
            });
            Task.WaitAll(tasks);
            tasks = new Task[2];
            tasks[0] = Task.Run(() =>
            {
                ctrlN1.Stop(processEpoch);
            });
            tasks[1] = Task.Run(() =>
            {
                ctrlN2.Stop(processEpoch);
            });
            Task.WaitAll(tasks);
            List<FSAnnouncement> results = new();
            resultStorage.Iterate(a => true, a => results.Add(a));
            Assert.AreEqual(announces.Count, results.Count);
            foreach (var a in announces)
            {
                results.Contains(a);
            }
        }
    }
}
