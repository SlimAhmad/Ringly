using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Brokers;

namespace Ringly.Trunking.Asterisk.Services.Monitoring.SpendAlerts;

public class TrunkSpendAlertService : ITrunkSpendAlertService
{
    private readonly ISipTrunkBroker sipTrunkBroker;
    private readonly ITrunkSpendAlertNotifier alertNotifier;
    private readonly ILoggingBroker loggingBroker;

    public TrunkSpendAlertService(
        ISipTrunkBroker sipTrunkBroker,
        ITrunkSpendAlertNotifier alertNotifier,
        ILoggingBroker loggingBroker)
    {
        this.sipTrunkBroker = sipTrunkBroker;
        this.alertNotifier = alertNotifier;
        this.loggingBroker = loggingBroker;
    }

    public async ValueTask PollAsync()
    {
        IReadOnlyList<string> trunkNames = await this.sipTrunkBroker.ListConfiguredTrunkNamesAsync();

        foreach (string trunkName in trunkNames)
        {
            await this.PollTrunkAsync(trunkName);
        }
    }

    // One trunk's failure (e.g. a broker error retrieving its config) must not stop alerting
    // for the rest — caught and logged per-trunk rather than letting PollAsync abort the pass.
    private async ValueTask PollTrunkAsync(string trunkName)
    {
        try
        {
            SipTrunkConfig config = await this.sipTrunkBroker.RetrieveTrunkConfigAsync(trunkName);
            TrunkCallLimitStatus status = await this.sipTrunkBroker.RetrieveSpendStatusAsync(trunkName);

            bool isOverConcurrencyLimit = status.ActiveCallCount >= config.MaxConcurrentCallsPerTrunk;

            bool isOverSpendLimit =
                config.MaxDailySpendUsd.HasValue && status.SpendTodayUsd >= config.MaxDailySpendUsd.Value;

            if (isOverConcurrencyLimit || isOverSpendLimit)
            {
                await this.alertNotifier.NotifyAsync(trunkName, status);
            }
        }
        catch (Exception exception)
        {
            await this.loggingBroker.LogErrorAsync(exception);
        }
    }
}
