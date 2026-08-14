using Microsoft.Extensions.Logging;

namespace Ringly.Samples.Maui;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string filePath;
    private readonly object writeLock = new();

    public FileLoggerProvider(string filePath)
    {
        this.filePath = filePath;

        try
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(
                $"=== log started {DateTimeOffset.UtcNow:O} ==={Environment.NewLine}");
            using var stream = new FileStream(this.filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            stream.Write(bytes, 0, bytes.Length);
        }
        catch (IOException)
        {
        }
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

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(line + Environment.NewLine);

            lock (writeLock)
            {
                // This log file is shared across separate OS processes (two instances of this
                // sample app, both logging to the same path for easier side-by-side tracing).
                // File.AppendAllText opens the file exclusively, so a write from the other
                // process landing at the same moment throws a sharing-violation IOException —
                // confirmed to crash the app outright, since logging must never be allowed to
                // take down real call handling. FileShare.ReadWrite lets both processes' writes
                // interleave instead of colliding, and any residual IO failure is swallowed here
                // rather than propagated.
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    stream.Write(bytes, 0, bytes.Length);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
