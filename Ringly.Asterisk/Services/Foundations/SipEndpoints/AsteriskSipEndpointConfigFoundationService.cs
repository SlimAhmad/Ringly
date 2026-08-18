using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.Asterisk.Services.Foundations.SipEndpoints;

public partial class AsteriskSipEndpointConfigFoundationService : IAsteriskSipEndpointConfigFoundationService
{
    private readonly IAsteriskBroker asteriskBroker;
    private readonly ILoggingBroker loggingBroker;

    public AsteriskSipEndpointConfigFoundationService(
        IAsteriskBroker asteriskBroker,
        ILoggingBroker loggingBroker)
    {
        this.asteriskBroker = asteriskBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask AddSipEndpointConfigAsync(SipEndpointConfig config) =>
    TryCatch(async () =>
    {
        ValidateSipEndpointConfig(config);

        // Asterisk's dynamic config PUT is an upsert (200, silently overwrites) — it has no
        // native conflict signal for an already-provisioned extension, confirmed against the
        // real ARI endpoint. Enforce collision rejection ourselves via a pre-insert existence
        // check rather than relying on a 409 that Asterisk will never send.
        if (await this.SipEndpointConfigExistsAsync(config.Extension))
        {
            throw new DuplicateExtensionException(
                new InvalidOperationException($"Extension '{config.Extension}' is already provisioned."));
        }

        await this.asteriskBroker.InsertSipEndpointConfigAsync(config);
    });

    public ValueTask RemoveSipEndpointConfigAsync(string extension) =>
    TryCatch(async () =>
    {
        ValidateExtension(extension);

        if (!await this.SipEndpointConfigExistsAsync(extension))
        {
            throw new ExtensionNotFoundException(
                new InvalidOperationException($"Extension '{extension}' is not provisioned."));
        }

        // Reverse of Insert's aor->auth->endpoint order, since endpoint references both aor and
        // auth by id. Each object removed independently (not one bundled broker call) so that
        // "endpoint" already being absent — expected today, given the known PUT bug documented on
        // InsertSipEndpointConfigAsync means it was likely never created — doesn't abort cleanup
        // of auth/aor, which DO exist and need removing.
        foreach (string objectType in new[] { "endpoint", "auth", "aor" })
        {
            await this.RemoveSipEndpointConfigObjectIfExistsAsync(objectType, extension);
        }
    });

    private async ValueTask RemoveSipEndpointConfigObjectIfExistsAsync(string objectType, string extension)
    {
        try
        {
            await this.asteriskBroker.RemoveSipEndpointConfigObjectAsync(objectType, extension);
        }
        catch (HttpResponseNotFoundException)
        {
            // Idempotent delete — already gone is not a failure.
        }
    }

    private async ValueTask<bool> SipEndpointConfigExistsAsync(string extension)
    {
        try
        {
            await this.asteriskBroker.RetrieveSipEndpointConfigAsync(extension);
            return true;
        }
        catch (HttpResponseNotFoundException)
        {
            return false;
        }
    }
}
