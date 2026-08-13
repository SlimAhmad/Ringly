using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService
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
