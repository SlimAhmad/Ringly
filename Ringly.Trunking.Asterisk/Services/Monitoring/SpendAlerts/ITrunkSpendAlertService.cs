namespace Ringly.Trunking.Asterisk.Services.Monitoring.SpendAlerts;

public interface ITrunkSpendAlertService
{
    // One full pass over every configured trunk — checks each against its own limits and
    // notifies on anomalies. The background loop (TrunkSpendAlertBackgroundService) just calls
    // this on an interval; kept separate so the actual decision logic is unit-testable without
    // a hosted-service test harness.
    ValueTask PollAsync();
}
