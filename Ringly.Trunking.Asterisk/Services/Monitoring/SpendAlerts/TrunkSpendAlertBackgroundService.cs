using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Ringly.Trunking.Asterisk.Brokers;

namespace Ringly.Trunking.Asterisk.Services.Monitoring.SpendAlerts;

// Thin polling loop — the actual decision logic lives in TrunkSpendAlertService, kept separate
// so it's unit-testable without a hosted-service test harness (consistent with how the rest of
// this codebase keeps testable logic out of thin dispatch/plumbing layers).
public class TrunkSpendAlertBackgroundService : BackgroundService
{
    private readonly ITrunkSpendAlertService trunkSpendAlertService;
    private readonly ILoggingBroker loggingBroker;
    private readonly TrunkSpendAlertOptions options;

    public TrunkSpendAlertBackgroundService(
        ITrunkSpendAlertService trunkSpendAlertService,
        ILoggingBroker loggingBroker,
        IOptions<TrunkSpendAlertOptions> options)
    {
        this.trunkSpendAlertService = trunkSpendAlertService;
        this.loggingBroker = loggingBroker;
        this.options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this.trunkSpendAlertService.PollAsync();
            }
            catch (Exception exception)
            {
                await this.loggingBroker.LogErrorAsync(exception);
            }

            try
            {
                await Task.Delay(this.options.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }
}
