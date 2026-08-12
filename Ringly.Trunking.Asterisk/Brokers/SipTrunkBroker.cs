using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTFulSense.Clients;

namespace Ringly.Trunking.Asterisk.Brokers;

public partial class SipTrunkBroker : ISipTrunkBroker
{
    private static readonly TimeSpan InitialReconnectDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(30);

    private readonly HttpClient httpClient;
    private readonly IRESTFulApiFactoryClient apiClient;
    private readonly Subject<JObject> ariEvents;
    private readonly SipTrunkOptions trunkOptions;

    public SipTrunkBroker(IOptions<SipTrunkOptions> options)
    {
        SipTrunkOptions trunkOptions = options.Value;
        this.trunkOptions = trunkOptions;

        // REST calls target the ARI HTTP API under /ari/ — see Ringly.Asterisk.Brokers
        // .AsteriskBroker for the same fix and why it matters (row #21).
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(new Uri(trunkOptions.BaseUrl.TrimEnd('/') + "/"), "ari/")
        };

        string credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{trunkOptions.Username}:{trunkOptions.Password}"));

        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        this.apiClient = new RESTFulApiFactoryClient(this.httpClient);
        this.ariEvents = new Subject<JObject>();

        _ = this.RunWithReconnectAsync(() => this.ConnectAriEventsAsync(trunkOptions));
    }

    private async Task RunWithReconnectAsync(Func<Task> connectAsync)
    {
        TimeSpan delay = InitialReconnectDelay;

        while (true)
        {
            try
            {
                await connectAsync();
                delay = InitialReconnectDelay;
            }
            catch
            {
                // Connection dropped or failed to establish — retry with backoff below.
            }

            await Task.Delay(delay);
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, MaxReconnectDelay.TotalSeconds));
        }
    }

    private async Task ConnectAriEventsAsync(SipTrunkOptions trunkOptions)
    {
        using var webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(BuildAriEventsUri(trunkOptions), CancellationToken.None);

        var buffer = new byte[8192];

        while (webSocket.State == WebSocketState.Open)
        {
            using var messageStream = new MemoryStream();
            WebSocketReceiveResult receiveResult;

            do
            {
                receiveResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                messageStream.Write(buffer, 0, receiveResult.Count);
            }
            while (!receiveResult.EndOfMessage);

            messageStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(messageStream);
            string json = await reader.ReadToEndAsync();
            this.ariEvents.OnNext(JObject.Parse(json));
        }
    }

    private static Uri BuildAriEventsUri(SipTrunkOptions trunkOptions)
    {
        var baseUri = new Uri(trunkOptions.BaseUrl);

        var eventsUriBuilder = new UriBuilder(baseUri)
        {
            Scheme = baseUri.Scheme == "https" ? "wss" : "ws",
            Path = "/ari/events",
            Query =
                $"app={Uri.EscapeDataString(trunkOptions.StasisAppName)}" +
                $"&api_key={Uri.EscapeDataString(trunkOptions.Username)}:{Uri.EscapeDataString(trunkOptions.Password)}"
        };

        return eventsUriBuilder.Uri;
    }

    private static readonly Func<string, ValueTask<string>> PassThroughDeserialization =
        content => ValueTask.FromResult(content ?? string.Empty);

    private async ValueTask<T> GetAsync<T>(string relativeUrl) =>
        await this.apiClient.SendHttpRequestAsync<T>(
            method: "GET",
            relativeUrl: relativeUrl,
            cancellationToken: CancellationToken.None);

    private async ValueTask<T> PostAsync<T>(string relativeUrl) =>
        await this.apiClient.SendHttpRequestAsync<T>(
            method: "POST",
            relativeUrl: relativeUrl,
            cancellationToken: CancellationToken.None);

    private async ValueTask DeleteAsync(string relativeUrl) =>
        await this.apiClient.SendHttpRequestAsync(
            method: "DELETE",
            relativeUrl: relativeUrl,
            cancellationToken: CancellationToken.None,
            deserailizationFunction: PassThroughDeserialization);

    private async ValueTask PutAsync<T>(string relativeUrl, T content) =>
        await this.apiClient.SendHttpRequestAsync<T, string>(
            method: "PUT",
            relativeUrl: relativeUrl,
            content: content,
            // RESTFulSense defaults to "text/json", which Asterisk's ARI HTTP server rejects
            // outright — confirmed against the real endpoint (row #21).
            mediaType: "application/json",
            deserializationFunction: PassThroughDeserialization);
}
