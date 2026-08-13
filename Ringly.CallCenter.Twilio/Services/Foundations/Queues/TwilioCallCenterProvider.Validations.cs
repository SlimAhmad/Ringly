using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Twilio.Models.Foundations.Queues.Exceptions;

namespace Ringly.CallCenter.Twilio.Services.Foundations.Queues;

public partial class TwilioCallCenterProvider
{
    private static void ValidateQueueConfig(QueueConfig config)
    {
        if (config is null)
        {
            throw new NullQueueConfigException();
        }

        var invalidQueueConfigException = new InvalidQueueConfigException();

        if (string.IsNullOrWhiteSpace(config.Name))
        {
            invalidQueueConfigException.UpsertDataList(
                key: nameof(QueueConfig.Name),
                value: "Value is required");
        }

        invalidQueueConfigException.ThrowIfContainsErrors();
    }
}
