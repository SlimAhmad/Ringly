using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

namespace Ringly.Asterisk.Services.Foundations.SipEndpoints;

public partial class AsteriskSipEndpointConfigFoundationService
{
    private static void ValidateSipEndpointConfig(SipEndpointConfig config)
    {
        if (config is null)
        {
            throw new NullSipEndpointConfigException();
        }

        var invalidSipEndpointConfigException = new InvalidSipEndpointConfigException();

        if (string.IsNullOrWhiteSpace(config.Extension))
        {
            invalidSipEndpointConfigException.UpsertDataList(
                key: nameof(SipEndpointConfig.Extension),
                value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(config.Password))
        {
            invalidSipEndpointConfigException.UpsertDataList(
                key: nameof(SipEndpointConfig.Password),
                value: "Value is required");
        }

        invalidSipEndpointConfigException.ThrowIfContainsErrors();
    }

    private static void ValidateExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            var invalidSipEndpointConfigException = new InvalidSipEndpointConfigException();

            invalidSipEndpointConfigException.UpsertDataList(
                key: "extension",
                value: "Value is required");

            invalidSipEndpointConfigException.ThrowIfContainsErrors();
        }
    }
}
