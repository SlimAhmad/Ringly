using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RESTFulSense.Clients;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker : IAsteriskBroker
{
    private static readonly TimeSpan InitialReconnectDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(30);

    private readonly HttpClient httpClient;
    private readonly IRESTFulApiFactoryClient apiClient;
    private readonly Subject<JsonElement> ariEvents;
    private readonly Subject<IReadOnlyDictionary<string, string>> amiEvents;
    private readonly AsteriskOptions asteriskOptions;

    public AsteriskBroker(IOptions<AsteriskOptions> options)
    {
        AsteriskOptions asteriskOptions = options.Value;
        this.asteriskOptions = asteriskOptions;

        // REST calls target the ARI HTTP API under /ari/ — asteriskOptions.BaseUrl itself stays
        // the bare server root (e.g. "http://host:8088") since BuildAriEventsUri and the AMI
        // TCP connection below both derive their own paths/host directly from it.
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(new Uri(asteriskOptions.BaseUrl.TrimEnd('/') + "/"), "ari/")
        };

        string credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{asteriskOptions.Username}:{asteriskOptions.Password}"));

        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        this.apiClient = new RESTFulApiFactoryClient(this.httpClient);

        this.ariEvents = new Subject<JsonElement>();
        this.amiEvents = new Subject<IReadOnlyDictionary<string, string>>();

        // Independent reconnect loops per §5.9 — a dropped AMI connection must not kill the ARI stream.
        _ = this.RunWithReconnectAsync(() => this.ConnectAriEventsAsync(asteriskOptions));
        _ = this.RunWithReconnectAsync(() => this.ConnectAmiEventsAsync(asteriskOptions));
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
                // Not business-exception handling: this keeps the reconnect loop alive per §5.9,
                // it does not translate or suppress a domain-meaningful failure.
            }

            await Task.Delay(delay);
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, MaxReconnectDelay.TotalSeconds));
        }
    }

    private async Task ConnectAriEventsAsync(AsteriskOptions asteriskOptions)
    {
        using var webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(BuildAriEventsUri(asteriskOptions), CancellationToken.None);

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
            JsonElement ariEvent = await JsonSerializer.DeserializeAsync<JsonElement>(messageStream);
            this.ariEvents.OnNext(ariEvent);
        }
    }

    private async Task ConnectAmiEventsAsync(AsteriskOptions asteriskOptions)
    {
        string host = new Uri(asteriskOptions.BaseUrl).Host;

        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, asteriskOptions.AmiPort);

        await using NetworkStream stream = tcpClient.GetStream();
        var writer = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true };
        var reader = new StreamReader(stream);

        await writer.WriteAsync(
            $"Action: Login\r\nUsername: {asteriskOptions.AmiUsername}\r\nSecret: {asteriskOptions.AmiSecret}\r\n\r\n");

        var currentBlock = new Dictionary<string, string>();
        string? line;

        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (line.Length == 0)
            {
                if (currentBlock.Count > 0)
                {
                    this.amiEvents.OnNext(currentBlock);
                    currentBlock = new Dictionary<string, string>();
                }

                continue;
            }

            int separatorIndex = line.IndexOf(':');

            if (separatorIndex > 0)
            {
                currentBlock[line[..separatorIndex].Trim()] = line[(separatorIndex + 1)..].Trim();
            }
        }
    }

    private static Uri BuildAriEventsUri(AsteriskOptions asteriskOptions)
    {
        var baseUri = new Uri(asteriskOptions.BaseUrl);

        var eventsUriBuilder = new UriBuilder(baseUri)
        {
            Scheme = baseUri.Scheme == "https" ? "wss" : "ws",
            Path = "/ari/events",
            Query =
                $"app={Uri.EscapeDataString(asteriskOptions.StasisAppName)}" +
                $"&api_key={Uri.EscapeDataString(asteriskOptions.Username)}:{Uri.EscapeDataString(asteriskOptions.Password)}"
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

    private async ValueTask PostAsync(string relativeUrl) =>
        await this.apiClient.SendHttpRequestAsync(
            method: "POST",
            relativeUrl: relativeUrl,
            cancellationToken: CancellationToken.None,
            deserailizationFunction: PassThroughDeserialization);

    private async ValueTask DeleteAsync(string relativeUrl) =>
        await this.apiClient.SendHttpRequestAsync(
            method: "DELETE",
            relativeUrl: relativeUrl,
            cancellationToken: CancellationToken.None,
            deserailizationFunction: PassThroughDeserialization);

    private async ValueTask PutAsync(string relativeUrl) =>
        await this.apiClient.SendHttpRequestAsync(
            method: "PUT",
            relativeUrl: relativeUrl,
            cancellationToken: CancellationToken.None,
            deserailizationFunction: PassThroughDeserialization);

    private async ValueTask PutAsync<T>(string relativeUrl, T content) =>
        await this.apiClient.SendHttpRequestAsync<T, string>(
            method: "PUT",
            relativeUrl: relativeUrl,
            content: content,
            // RESTFulSense defaults to "text/json", which Asterisk's ARI HTTP server rejects
            // outright ("failed field value validation") — confirmed against the real endpoint.
            mediaType: "application/json",
            deserializationFunction: PassThroughDeserialization);

    private async ValueTask PostAsync<T>(string relativeUrl, T content) =>
        await this.apiClient.SendHttpRequestAsync<T, string>(
            method: "POST",
            relativeUrl: relativeUrl,
            content: content,
            mediaType: "application/json",
            deserializationFunction: PassThroughDeserialization);
}
