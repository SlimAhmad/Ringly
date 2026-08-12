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
