using System;
using System.Threading;
using static Neo.Utility;

namespace Neo.FileStorage.Storage.Services.Reputaion.Local.Control
{
    public class ReportContext
    {
        public CancellationToken Cancel;
        public ulong Epoch;
        public Controller Controller;

        public void Report()
        {
            var ctx = new IterationContext
            {
                Cancellation = Cancel,
                Epoch = Epoch,
            };
            try
            {
                var iterator = Controller.LocalTrustStorage.InitIterator(ctx);
                var writer = Controller.Router.InitWriter(ctx);
                iterator.Iterate(t =>
                {
                    Cancel.ThrowIfCancellationRequested();
                    writer.Write(t);
                });
                writer.Close();
            }
            catch (Exception e)
            {
                Log("Reputation.Contoller", LogLevel.Debug, $"report failed, error={e.Message}");
            }
        }
    }
}
