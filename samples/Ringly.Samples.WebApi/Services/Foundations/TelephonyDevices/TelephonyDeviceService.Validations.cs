using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceService
{
    private static void ValidateTelephonyDeviceOnAdd(TelephonyDevice telephonyDevice)
    {
        ValidateTelephonyDeviceIsNotNull(telephonyDevice);

        Validate(
            (Rule: IsInvalid(telephonyDevice.Id), Parameter: nameof(TelephonyDevice.Id)),
            (Rule: IsInvalid(telephonyDevice.IdentityId), Parameter: nameof(TelephonyDevice.IdentityId)),
            (Rule: IsInvalid(telephonyDevice.Platform), Parameter: nameof(TelephonyDevice.Platform)));
    }

    private static void ValidateTelephonyDeviceOnModify(TelephonyDevice telephonyDevice)
    {
        ValidateTelephonyDeviceIsNotNull(telephonyDevice);

        Validate(
            (Rule: IsInvalid(telephonyDevice.Id), Parameter: nameof(TelephonyDevice.Id)),
            (Rule: IsInvalid(telephonyDevice.IdentityId), Parameter: nameof(TelephonyDevice.IdentityId)),
            (Rule: IsInvalid(telephonyDevice.Platform), Parameter: nameof(TelephonyDevice.Platform)));
    }

    private static void ValidateTelephonyDeviceId(Guid telephonyDeviceId) =>
        Validate((Rule: IsInvalid(telephonyDeviceId), Parameter: nameof(TelephonyDevice.Id)));

    private static void ValidateIdentityId(Guid identityId) =>
        Validate((Rule: IsInvalid(identityId), Parameter: nameof(TelephonyDevice.IdentityId)));

    private static void ValidateTelephonyDeviceIsNotNull(TelephonyDevice? telephonyDevice)
    {
        if (telephonyDevice is null)
        {
            throw new NullTelephonyDeviceException();
        }
    }

    private static void ValidateStorageTelephonyDeviceExists(
        TelephonyDevice? maybeTelephonyDevice, Guid telephonyDeviceId)
    {
        if (maybeTelephonyDevice is null)
        {
            throw new NotFoundTelephonyDeviceException(telephonyDeviceId);
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
        var invalidTelephonyDeviceException = new InvalidTelephonyDeviceException();

        foreach ((dynamic rule, string parameter) in validations)
        {
            if (rule.Condition)
            {
                invalidTelephonyDeviceException.UpsertDataList(key: parameter, value: rule.Message);
            }
        }

        invalidTelephonyDeviceException.ThrowIfContainsErrors();
    }
}
