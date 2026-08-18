using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Services.Foundations.SipEndpoints;

public interface IAsteriskSipEndpointConfigFoundationService
{
    ValueTask AddSipEndpointConfigAsync(SipEndpointConfig config);
    ValueTask RemoveSipEndpointConfigAsync(string extension);
}
