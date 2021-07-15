using Microsoft.Extensions.Logging;

namespace Neo.FileStorage.Storage.gRPC
{
    public class LoggerProvider : ILoggerProvider
    {
        ILogger ILoggerProvider.CreateLogger(string categoryName)
        {
            return new Logger(categoryName);
        }

        public void Dispose() { }
    }
}
