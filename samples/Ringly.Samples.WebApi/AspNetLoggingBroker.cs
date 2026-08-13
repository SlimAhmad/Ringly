using Ringly.Asterisk.Brokers;

namespace Ringly.Samples.WebApi;

// Bridges Ringly.Asterisk's ILoggingBroker onto ASP.NET Core's own ILogger, so validation and
// dependency failures from Ringly's foundation services show up in the host's normal log output.
public class AspNetLoggingBroker(ILogger<AspNetLoggingBroker> logger) : ILoggingBroker
{
    public ValueTask LogInformationAsync(string message) => Log(LogLevel.Information, message);
    public ValueTask LogTraceAsync(string message) => Log(LogLevel.Trace, message);
    public ValueTask LogDebugAsync(string message) => Log(LogLevel.Debug, message);
    public ValueTask LogWarningAsync(string message) => Log(LogLevel.Warning, message);
    public ValueTask LogErrorAsync(Exception exception) => Log(LogLevel.Error, exception.Message, exception);
    public ValueTask LogCriticalAsync(Exception exception) => Log(LogLevel.Critical, exception.Message, exception);

    private ValueTask Log(LogLevel level, string message, Exception? exception = null)
    {
        logger.Log(level, exception, "{Message}", message);
        return ValueTask.CompletedTask;
    }
}
