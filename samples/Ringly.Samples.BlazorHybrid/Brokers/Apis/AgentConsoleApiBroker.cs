using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RESTFulSense.Clients;
using Ringly.Samples.BlazorHybrid.Models.Agents;

namespace Ringly.Samples.BlazorHybrid.Brokers.Apis;

public class AgentConsoleApiBroker : IAgentConsoleApiBroker
{
    private readonly IRESTFulApiFactoryClient apiClient;
    private readonly HttpClient streamingHttpClient;

    // Same base URL as SupportApiBroker (Ringly.Samples.WebApi) — reuses SupportApiOptions rather
    // than a duplicate options type, since both brokers target the same server.
    public AgentConsoleApiBroker(IOptions<SupportApiOptions> options)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(options.Value.BaseUrl) };
        this.apiClient = new RESTFulApiFactoryClient(httpClient);

        // Separate HttpClient for the SSE stream — IRESTFulApiFactoryClient has no
        // streaming-response concept (its methods all buffer a complete request/response), and a
        // stream that stays open for the app's lifetime shouldn't share a client instance with
        // ordinary short-lived request/response calls.
        //
        // Explicit SocketsHttpHandler, not the platform default — confirmed live on Android: raw
        // curl against the exact same broadcasts endpoint succeeded every time (real 200,
        // text/event-stream, immediately), yet this client kept throwing on every attempt. .NET
        // for Android defaults to Xamarin.Android's native AndroidMessageHandler
        // (UseNativeHttpHandler, on by default since the .NET 6 templates), which is built on
        // Java's HttpURLConnection/OkHttp and has known real-world issues with true long-lived
        // chunked streaming responses (SSE) — it buffers or fails in ways a normal
        // request/response call never surfaces, which is why ordinary calls through the
        // RESTFulSense-backed apiClient above work fine while only this streaming client fails.
        // The fully-managed SocketsHttpHandler doesn't have that limitation.
        this.streamingHttpClient = new HttpClient(new SocketsHttpHandler())
        {
            BaseAddress = new Uri(options.Value.BaseUrl),
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    public async ValueTask PostAvailabilityAsync(string agentAppName, bool isAvailable) =>
        await this.apiClient.PostContentAsync<object, object>(
            relativeUrl: $"api/agents/{agentAppName}/availability",
            content: new { isAvailable },
            mediaType: "application/json");

    public async ValueTask PostClaimAsync(string agentAppName, string channelId) =>
        await this.apiClient.PostContentAsync<object, object>(
            relativeUrl: $"api/agents/{agentAppName}/claim/{channelId}",
            content: new { },
            mediaType: "application/json");

    // Reads Server-Sent Events ("data: {json}\n\n" frames) directly off the response stream — no
    // browser EventSource involved, since this is a native/server .NET process, not JS running in
    // a browser. Mirrors AgentsController.GetBroadcastsAsync's own framing exactly.
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
