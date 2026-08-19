using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallService
{
    private static void ValidateTelephonyCallOnAdd(TelephonyCall telephonyCall)
    {
        ValidateTelephonyCallIsNotNull(telephonyCall);

        Validate(
            (Rule: IsInvalid(telephonyCall.Id), Parameter: nameof(TelephonyCall.Id)),
            (Rule: IsInvalid(telephonyCall.CallerIdentityId), Parameter: nameof(TelephonyCall.CallerIdentityId)),
            (Rule: IsInvalid(telephonyCall.RecipientIdentityId), Parameter: nameof(TelephonyCall.RecipientIdentityId)),
            (Rule: IsInvalid(telephonyCall.Status), Parameter: nameof(TelephonyCall.Status)));
    }

    private static void ValidateTelephonyCallOnModify(TelephonyCall telephonyCall)
    {
        ValidateTelephonyCallIsNotNull(telephonyCall);

        Validate(
            (Rule: IsInvalid(telephonyCall.Id), Parameter: nameof(TelephonyCall.Id)),
            (Rule: IsInvalid(telephonyCall.CallerIdentityId), Parameter: nameof(TelephonyCall.CallerIdentityId)),
            (Rule: IsInvalid(telephonyCall.RecipientIdentityId), Parameter: nameof(TelephonyCall.RecipientIdentityId)),
            (Rule: IsInvalid(telephonyCall.Status), Parameter: nameof(TelephonyCall.Status)));
    }

    private static void ValidateTelephonyCallId(Guid telephonyCallId) =>
        Validate((Rule: IsInvalid(telephonyCallId), Parameter: nameof(TelephonyCall.Id)));

    private static void ValidateCallerIdentityId(Guid callerIdentityId) =>
        Validate((Rule: IsInvalid(callerIdentityId), Parameter: nameof(TelephonyCall.CallerIdentityId)));

    private static void ValidateTelephonyCallIsNotNull(TelephonyCall? telephonyCall)
    {
        if (telephonyCall is null)
        {
            throw new NullTelephonyCallException();
        }
    }

    private static void ValidateStorageTelephonyCallExists(TelephonyCall? maybeTelephonyCall, Guid telephonyCallId)
    {
        if (maybeTelephonyCall is null)
        {
            throw new NotFoundTelephonyCallException(telephonyCallId);
        }
    }

    private static dynamic IsInvalid(Guid id) => new
    {
        Condition = id == default,
        Message = "Id is required"
    };

    private static dynamic IsInvalid<TEnum>(TEnum value) where TEnum : struct, Enum => new
    {
        Condition = !Enum.IsDefined(value),
        Message = "Value is not recognized"
    };

    private static void Validate(params (dynamic Rule, string Parameter)[] validations)
    {
        var invalidTelephonyCallException = new InvalidTelephonyCallException();

        foreach ((dynamic rule, string parameter) in validations)
        {
            if (rule.Condition)
            {
                invalidTelephonyCallException.UpsertDataList(key: parameter, value: rule.Message);
            }
        }

        invalidTelephonyCallException.ThrowIfContainsErrors();
    }
}
