using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.Storage.Services.Container.Announcement.Storage;
using static Neo.FileStorage.Storage.Services.Container.Announcement.Storage.Helper;
using static Neo.FileStorage.Storage.Tests.Helper;

namespace Neo.FileStorage.Tests.Services.Container.Announcement
{
    [TestClass]
    public class UT_Storage
    {
        [TestMethod]
        public void TestStorage()
        {
            const ulong epoch = 13;
            var a = RandomAnnouncement();
            a.Epoch = epoch;
            var s = new AnnouncementStorage();
            const int opinionsNum = 100;
            List<ulong> opinions = new();
            for (int i = 0; i < opinionsNum; i++)
            {
                var val = RandomUInt64();
                opinions.Add(val);
                a.UsedSpace = val;
                s.Put(a);
            }
            int counter = 0;
            var estimation = FinalEstimation(opinions);
            s.Iterate(ai => ai.Epoch == epoch, ai =>
            {
                counter++;
                Assert.AreEqual(epoch, ai.Epoch);
                Assert.AreEqual(a.ContainerId, ai.ContainerId);
                Assert.AreEqual(estimation, ai.UsedSpace);
            });
        }
    }
}
