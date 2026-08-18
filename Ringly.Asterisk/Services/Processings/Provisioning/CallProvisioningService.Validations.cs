using Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

namespace Ringly.Asterisk.Services.Processings.Provisioning;

public partial class CallProvisioningService
{
    private static void ValidateClientId(Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            var invalidSipCredentialsException = new InvalidSipCredentialsException();

            invalidSipCredentialsException.UpsertDataList(
                key: nameof(clientId),
                value: "Value is required");

            invalidSipCredentialsException.ThrowIfContainsErrors();
        }
    }

    private static void ValidateExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            var invalidSipCredentialsException = new InvalidSipCredentialsException();

            invalidSipCredentialsException.UpsertDataList(
                key: nameof(extension),
                value: "Value is required");

            invalidSipCredentialsException.ThrowIfContainsErrors();
        }
    }
}
