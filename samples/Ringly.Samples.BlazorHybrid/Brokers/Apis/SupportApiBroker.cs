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
        var httpClient = new HttpClient
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
