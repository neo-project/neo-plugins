using System;
using Microsoft.Extensions.Logging;
using MLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Neo.FileStorage.Storage.gRPC
{
    public class NonState : IDisposable
    {
        public void Dispose() { }
    }

    public class Logger : ILogger
    {
        private const string DefaultCategory = "null";
        private readonly string categoryName = DefaultCategory;

        public Logger() { }

        public Logger(string category)
        {
            categoryName = category;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new NonState();
        }

        public bool IsEnabled(MLogLevel logLevel)
        {
            return logLevel != MLogLevel.None;
        }

        private LogLevel ToNeoLogLevel(MLogLevel ll)
        {
            if (ll == MLogLevel.None) throw new NotSupportedException();
            byte val = (byte)ll;
            if (val == 0) return LogLevel.Debug;
            val -= 1;
            return (LogLevel)val;
        }

        public void Log<TState>(MLogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            string log = formatter is null ? $"{state}" + exception is null ? "" : $", exception: {exception}" : formatter(state, exception);
            Utility.Log(categoryName, ToNeoLogLevel(logLevel), log);
        }
    }
}
