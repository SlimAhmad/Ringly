using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;

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

        await this.asteriskBroker.InsertSipEndpointConfigAsync(config);
    });
}
