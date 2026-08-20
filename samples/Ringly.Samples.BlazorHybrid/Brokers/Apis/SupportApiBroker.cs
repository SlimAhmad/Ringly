using Microsoft.Extensions.Options;
using RESTFulSense.Clients;
using Ringly.Client.Abstractions.Models;
using Ringly.Samples.BlazorHybrid.Models.Support;

namespace Ringly.Samples.BlazorHybrid.Brokers.Apis;

public class SupportApiBroker : ISupportApiBroker
{
    private readonly IRESTFulApiFactoryClient apiClient;

    public SupportApiBroker(IOptions<SupportApiOptions> options)
    {
        // Explicit SocketsHttpHandler, not the platform default — see AgentConsoleApiBroker's own
        // comment for the full story on .NET for Android's native AndroidMessageHandler. That fix
        // was found chasing a stuck SSE stream, but the same handler misbehaves on any
        // slow-to-complete request, not just streams: confirmed live that PostSupportRouteAsync
        // ("Request support") failed with a generic "connection failure" on Android specifically
        // — PostRouteAsync's own RouteToQueueAsync deliberately blocks server-side until the
        // customer answers the call it just originated (up to 30s), and the native handler drops
        // the connection well before that, unlike ordinary fast calls (e.g. PostCredentialsAsync)
        // which never hit this.
        var httpClient = new HttpClient(new SocketsHttpHandler())
        {
            BaseAddress = new Uri(options.Value.BaseUrl)
        };

        this.apiClient = new RESTFulApiFactoryClient(httpClient);
    }

    public async ValueTask<SipCredentials> PostCredentialsAsync(Guid clientId) =>
        await this.PostAsync<SipCredentials>($"api/clients/{clientId}/credentials");

    public async ValueTask<SupportRouteResult> PostSupportRouteAsync(Guid clientId, string queueName) =>
        await this.PostAsync<SupportRouteResult>(
            $"api/support/{clientId}/route?queueName={Uri.EscapeDataString(queueName)}");

    // Both target routes are bodyless POSTs — RESTFulSense's generic PostContentAsync still
    // requires a content argument, so an empty object stands in for "no body". Explicit
    // mediaType: "application/json" per this repo's established RESTFulSense convention.
    private async ValueTask<TResult> PostAsync<TResult>(string relativeUrl) =>
        await this.apiClient.PostContentAsync<object, TResult>(
            relativeUrl: relativeUrl,
            content: new { },
            mediaType: "application/json");
}
