using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Twilio.Services.Foundations.CallSessions;

public partial class TwilioCallProvider
{
    private static void ValidateRouteToQueueRequest(Guid customerId, string queueName)
    {
        var invalidRouteToQueueRequestException = new InvalidRouteToQueueRequestException();

        if (customerId == Guid.Empty)
        {
            invalidRouteToQueueRequestException.UpsertDataList(
                key: nameof(customerId),
                value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(queueName))
        {
            invalidRouteToQueueRequestException.UpsertDataList(
                key: nameof(queueName),
                value: "Value is required");
        }

        invalidRouteToQueueRequestException.ThrowIfContainsErrors();
    }
}
