using System;
using System.Collections.Generic;
using Neo.FileStorage.Storage.Services.Container.Announcement.Route;
using FSAnnouncement = Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types.Announcement;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Storage
{
    /// <summary>
    /// Storage represents in-memory storage of
    /// UsedSpaceAnnouncement values.
    ///
    /// The write operation has the usual behavior - to save
    /// the next number of used container space for a specific epoch.
    /// All values related to one key (epoch, container ID) are stored
    /// as a list.
    ///
    /// Storage also provides an iterator interface, into the handler
    /// of which the final score is passed, built on all values saved
    /// at the time of the call. Currently the only possible estimation
    /// formula is used - the average between 10th and 90th percentile.
    ///
    /// For correct operation, Storage must be created
    /// using the constructor (New) based on the required parameters
    /// and optional components. After successful creation,
    /// Storage is immediately ready to work through API.
    /// </summary>
    public class AnnouncementStorage : IWriter
    {
        private Dictionary<ulong, AnnounceUsedSpaceEstimation> mItems = new();

        public void Put(FSAnnouncement announcement)
        {
            lock (mItems)
            {
                bool exists = mItems.TryGetValue(announcement.Epoch, out AnnounceUsedSpaceEstimation estimation);
                if (!exists)
                {
                    estimation = new()
                    {
                        Announcement = announcement,
                        Sizes = new(),
                    };
                    mItems[announcement.Epoch] = estimation;
                }
                estimation.Sizes.Add(announcement.UsedSpace);
            }
        }

        public void Close() { }

        public void Iterate(Func<FSAnnouncement, bool> filter, Action<FSAnnouncement> handler)
        {
            foreach (var (_, estimation) in mItems)
            {
                if (estimation.Announcement is not null)
                {
                    estimation.Announcement.UsedSpace = Helper.FinalEstimation(estimation.Sizes);
                    try
                    {
                        handler(estimation.Announcement);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
        }


    }
}
