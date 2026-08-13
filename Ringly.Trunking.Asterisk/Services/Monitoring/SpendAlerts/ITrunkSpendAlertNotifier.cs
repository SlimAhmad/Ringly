using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Services.Monitoring.SpendAlerts;

// §8.7 item 13 — "a human gets notified on anomalous spend, not just logged silently." Not
// doc-specified which channel (email/Slack/PagerDuty/SMS) — this is the abstraction a real
// notification channel plugs into. Only a logging-based default ships with this row (see
// LoggingTrunkSpendAlertNotifier); replace it with a real paging/messaging integration before
// relying on this for production toll-fraud detection.
public interface ITrunkSpendAlertNotifier
{
    ValueTask NotifyAsync(string trunkName, TrunkCallLimitStatus status);
}
