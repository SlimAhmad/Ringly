namespace Ringly.Trunking.Asterisk.Services.Monitoring.SpendAlerts;

public class TrunkSpendAlertOptions
{
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(5);
}
