using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Brokers;

namespace Ringly.Trunking.Asterisk.Services.Monitoring.SpendAlerts;

// Minimal default so alerting is wired end-to-end out of the box — logs at Critical (not the
// usual Error), the same severity CreateAndLogCriticalDependencyException uses elsewhere in
// this codebase to mean "needs human attention." On its own this only helps if something is
// actually watching Critical-level logs; swap in a real paging/messaging notifier for
// production toll-fraud detection.
public class LoggingTrunkSpendAlertNotifier : ITrunkSpendAlertNotifier
{
    private readonly ILoggingBroker loggingBroker;

    public LoggingTrunkSpendAlertNotifier(ILoggingBroker loggingBroker) =>
        this.loggingBroker = loggingBroker;

    public async ValueTask NotifyAsync(string trunkName, TrunkCallLimitStatus status) =>
        await this.loggingBroker.LogCriticalAsync(
            new Exception(
                $"Trunk '{trunkName}' spend/concurrency anomaly: " +
                $"ActiveCallCount={status.ActiveCallCount}, SpendTodayUsd={status.SpendTodayUsd}"));
}
