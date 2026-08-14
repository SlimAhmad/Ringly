using Microsoft.Extensions.Logging;

namespace Ringly.Samples.Maui;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string filePath;
    private readonly object writeLock = new();

    public FileLoggerProvider(string filePath)
    {
        this.filePath = filePath;
        File.WriteAllText(this.filePath, $"=== log started {DateTimeOffset.UtcNow:O} ==={Environment.NewLine}");
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, this.filePath, this.writeLock);

    public void Dispose()
    {
    }

    private sealed class FileLogger(string categoryName, string filePath, object writeLock) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string line = $"{DateTimeOffset.UtcNow:HH:mm:ss.fff} [{logLevel}] {categoryName}: {formatter(state, exception)}";

            if (exception is not null)
            {
                line += $"{Environment.NewLine}{exception}";
            }

            lock (writeLock)
            {
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
        }
    }
}
