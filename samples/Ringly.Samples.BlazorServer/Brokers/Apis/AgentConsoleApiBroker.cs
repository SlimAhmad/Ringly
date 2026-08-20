using System.Runtime.CompilerServices;
using System.Text.Json;
using RESTFulSense.Clients;
using Ringly.Samples.BlazorServer.Models.Agents;

namespace Ringly.Samples.BlazorServer.Brokers.Apis;

public class AgentConsoleApiBroker : IAgentConsoleApiBroker
{
    private readonly IRESTFulApiFactoryClient apiClient;
    private readonly HttpClient streamingHttpClient;

    // Same base URL as SupportApiBroker (Ringly.Samples.WebApi) — reads the same "WebApiClient"
    // configuration section rather than a duplicate options type.
    public AgentConsoleApiBroker(IConfiguration configuration)
    {
        string baseUrl = configuration.GetValue<string>("WebApiClient:BaseUrl") ?? "http://localhost:5000";

        var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        this.apiClient = new RESTFulApiFactoryClient(httpClient);

        // Separate HttpClient for the SSE stream — see Ringly.Samples.BlazorHybrid's own
        // AgentConsoleApiBroker.cs for why this can't share the request/response client.
        this.streamingHttpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    public async ValueTask PostAvailabilityAsync(string agentAppName, bool isAvailable) =>
        await this.apiClient.PostContentAsync<object, object>(
            relativeUrl: $"api/agents/{agentAppName}/availability",
            content: new { isAvailable },
            mediaType: "application/json");

    public async ValueTask<ClaimResult> PostClaimAsync(string agentAppName, string channelId) =>
        await this.apiClient.PostContentAsync<object, ClaimResult>(
            relativeUrl: $"api/agents/{agentAppName}/claim/{channelId}",
            content: new { },
            mediaType: "application/json");

    public async IAsyncEnumerable<AgentBroadcastInfo> StreamBroadcastsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/agents/broadcasts");

        using HttpResponseMessage response = await this.streamingHttpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
            {
                yield break;
            }

            if (!line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            string json = line["data: ".Length..];

            AgentBroadcastInfo? broadcastInfo = JsonSerializer.Deserialize<AgentBroadcastInfo>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (broadcastInfo is not null)
            {
                yield return broadcastInfo;
            }
        }
    }
}
