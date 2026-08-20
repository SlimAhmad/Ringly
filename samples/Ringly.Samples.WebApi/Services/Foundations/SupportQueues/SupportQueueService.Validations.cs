using Ringly.Samples.WebApi.Models.Foundations.SupportQueues;
using Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

namespace Ringly.Samples.WebApi.Services.Foundations.SupportQueues;

public partial class SupportQueueService
{
    private static void ValidateSupportQueueOnAdd(SupportQueue supportQueue)
    {
        ValidateSupportQueueIsNotNull(supportQueue);

        Validate(
            (Rule: IsInvalid(supportQueue.Id), Parameter: nameof(SupportQueue.Id)),
            (Rule: IsInvalid(supportQueue.QueueName), Parameter: nameof(SupportQueue.QueueName)),
            (Rule: IsInvalid(supportQueue.BridgeId), Parameter: nameof(SupportQueue.BridgeId)));
    }

    private static void ValidateSupportQueueOnModify(SupportQueue supportQueue)
    {
        ValidateSupportQueueIsNotNull(supportQueue);

        Validate(
            (Rule: IsInvalid(supportQueue.Id), Parameter: nameof(SupportQueue.Id)),
            (Rule: IsInvalid(supportQueue.QueueName), Parameter: nameof(SupportQueue.QueueName)),
            (Rule: IsInvalid(supportQueue.BridgeId), Parameter: nameof(SupportQueue.BridgeId)));
    }

    private static void ValidateSupportQueueId(Guid supportQueueId) =>
        Validate((Rule: IsInvalid(supportQueueId), Parameter: nameof(SupportQueue.Id)));

    private static void ValidateQueueName(string queueName) =>
        Validate((Rule: IsInvalid(queueName), Parameter: nameof(SupportQueue.QueueName)));

    private static void ValidateSupportQueueIsNotNull(SupportQueue? supportQueue)
    {
        if (supportQueue is null)
        {
            throw new NullSupportQueueException();
        }
    }

    private static void ValidateStorageSupportQueueExists(
        SupportQueue? maybeSupportQueue, Guid supportQueueId)
    {
        if (maybeSupportQueue is null)
        {
            throw new NotFoundSupportQueueException(supportQueueId);
        }
    }

    private static dynamic IsInvalid(Guid id) => new
    {
        Condition = id == default,
        Message = "Id is required"
    };

    private static dynamic IsInvalid(string text) => new
    {
        Condition = string.IsNullOrWhiteSpace(text),
        Message = "Text is required"
    };

    private static void Validate(params (dynamic Rule, string Parameter)[] validations)
    {
        var invalidSupportQueueException = new InvalidSupportQueueException();

        foreach ((dynamic rule, string parameter) in validations)
        {
            if (rule.Condition)
            {
                invalidSupportQueueException.UpsertDataList(key: parameter, value: rule.Message);
            }
        }

        invalidSupportQueueException.ThrowIfContainsErrors();
    }
}
