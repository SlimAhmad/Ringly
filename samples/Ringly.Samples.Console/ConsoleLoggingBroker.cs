using Ringly.Twilio.Brokers;

namespace Ringly.Samples.Console;

public class ConsoleLoggingBroker : ILoggingBroker
{
    public ValueTask LogInformationAsync(string message) => Log(message);
    public ValueTask LogTraceAsync(string message) => Log(message);
    public ValueTask LogDebugAsync(string message) => Log(message);
    public ValueTask LogWarningAsync(string message) => Log(message);
    public ValueTask LogErrorAsync(Exception exception) => Log($"ERROR: {exception.Message}");
    public ValueTask LogCriticalAsync(Exception exception) => Log($"CRITICAL: {exception.Message}");

    private static ValueTask Log(string message)
    {
        System.Console.WriteLine($"[Ringly] {message}");
        return ValueTask.CompletedTask;
    }
}
