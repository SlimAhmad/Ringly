using Ringly.Client.Abstractions.Models;
using Ringly.Samples.BlazorServer.Models.Support;

namespace Ringly.Samples.BlazorServer.Brokers.Apis;

// Liaison between this app and Ringly.Samples.WebApi's ClientsController/SupportController — per
// the-standard-architecture's broker rules, no business logic here, just the HTTP calls
// themselves (provisioning + cold-support routing), matching those controllers' real routes.
public interface ISupportApiBroker
{
    ValueTask<SipCredentials> PostCredentialsAsync(Guid clientId);
    ValueTask<SupportRouteResult> PostSupportRouteAsync(Guid clientId, string queueName);
}
