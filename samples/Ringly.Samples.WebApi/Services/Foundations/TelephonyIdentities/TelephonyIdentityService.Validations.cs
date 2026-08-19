using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityService
{
    private static void ValidateTelephonyIdentityOnAdd(TelephonyIdentity telephonyIdentity)
    {
        ValidateTelephonyIdentityIsNotNull(telephonyIdentity);

        Validate(
            (Rule: IsInvalid(telephonyIdentity.Id), Parameter: nameof(TelephonyIdentity.Id)),
            (Rule: IsInvalid(telephonyIdentity.UserId), Parameter: nameof(TelephonyIdentity.UserId)),
            (Rule: IsInvalid(telephonyIdentity.SipUsername), Parameter: nameof(TelephonyIdentity.SipUsername)),
            (Rule: IsInvalid(telephonyIdentity.SipCredential), Parameter: nameof(TelephonyIdentity.SipCredential)),
            (Rule: IsInvalid(telephonyIdentity.Type), Parameter: nameof(TelephonyIdentity.Type)),
            (Rule: IsInvalid(telephonyIdentity.Status), Parameter: nameof(TelephonyIdentity.Status)));
    }

    private static void ValidateTelephonyIdentityOnModify(TelephonyIdentity telephonyIdentity)
    {
        ValidateTelephonyIdentityIsNotNull(telephonyIdentity);

        Validate(
            (Rule: IsInvalid(telephonyIdentity.Id), Parameter: nameof(TelephonyIdentity.Id)),
            (Rule: IsInvalid(telephonyIdentity.UserId), Parameter: nameof(TelephonyIdentity.UserId)),
            (Rule: IsInvalid(telephonyIdentity.SipUsername), Parameter: nameof(TelephonyIdentity.SipUsername)),
            (Rule: IsInvalid(telephonyIdentity.SipCredential), Parameter: nameof(TelephonyIdentity.SipCredential)),
            (Rule: IsInvalid(telephonyIdentity.Type), Parameter: nameof(TelephonyIdentity.Type)),
            (Rule: IsInvalid(telephonyIdentity.Status), Parameter: nameof(TelephonyIdentity.Status)));
    }

    private static void ValidateTelephonyIdentityId(Guid telephonyIdentityId) =>
        Validate((Rule: IsInvalid(telephonyIdentityId), Parameter: nameof(TelephonyIdentity.Id)));

    private static void ValidateUserId(Guid userId) =>
        Validate((Rule: IsInvalid(userId), Parameter: nameof(TelephonyIdentity.UserId)));

    private static void ValidateTelephonyIdentityIsNotNull(TelephonyIdentity? telephonyIdentity)
    {
        if (telephonyIdentity is null)
        {
            throw new NullTelephonyIdentityException();
        }
    }

    private static void ValidateStorageTelephonyIdentityExists(
        TelephonyIdentity? maybeTelephonyIdentity, Guid telephonyIdentityId)
    {
        if (maybeTelephonyIdentity is null)
        {
            throw new NotFoundTelephonyIdentityException(telephonyIdentityId);
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

    private static dynamic IsInvalid<TEnum>(TEnum value) where TEnum : struct, Enum => new
    {
        Condition = !Enum.IsDefined(value),
        Message = "Value is not recognized"
    };

    private static void Validate(params (dynamic Rule, string Parameter)[] validations)
    {
        var invalidTelephonyIdentityException = new InvalidTelephonyIdentityException();

        foreach ((dynamic rule, string parameter) in validations)
        {
            if (rule.Condition)
            {
                invalidTelephonyIdentityException.UpsertDataList(key: parameter, value: rule.Message);
            }
        }

        invalidTelephonyIdentityException.ThrowIfContainsErrors();
    }
}
