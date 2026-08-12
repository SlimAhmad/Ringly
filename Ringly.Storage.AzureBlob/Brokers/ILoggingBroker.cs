namespace Ringly.Storage.AzureBlob.Brokers;

public interface ILoggingBroker
{
    ValueTask LogInformationAsync(string message);
    ValueTask LogTraceAsync(string message);
    ValueTask LogDebugAsync(string message);
    ValueTask LogWarningAsync(string message);
    ValueTask LogErrorAsync(Exception exception);
    ValueTask LogCriticalAsync(Exception exception);
}
