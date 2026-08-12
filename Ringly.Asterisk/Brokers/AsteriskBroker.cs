using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker : IAsteriskBroker
{
    private readonly HttpClient ariClient;
    private readonly Subject<JsonElement> ariEvents;
    private readonly AsteriskOptions asteriskOptions;

    public AsteriskBroker(IOptions<AsteriskOptions> options)
    {
        AsteriskOptions asteriskOptions = options.Value;
        this.asteriskOptions = asteriskOptions;

        this.ariClient = new HttpClient
        {
            BaseAddress = new Uri(asteriskOptions.BaseUrl)
        };

        string credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{asteriskOptions.Username}:{asteriskOptions.Password}"));

        this.ariClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        this.ariEvents = new Subject<JsonElement>();
        _ = this.ConnectAriEventsAsync(asteriskOptions);
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

    private async ValueTask<T> PostAsync<T>(string relativeUrl)
    {
        HttpResponseMessage response = await this.ariClient.PostAsync(relativeUrl, content: null);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async ValueTask PostAsync(string relativeUrl)
    {
        HttpResponseMessage response = await this.ariClient.PostAsync(relativeUrl, content: null);
        response.EnsureSuccessStatusCode();
    }

    private async ValueTask DeleteAsync(string relativeUrl)
    {
        HttpResponseMessage response = await this.ariClient.DeleteAsync(relativeUrl);
        response.EnsureSuccessStatusCode();
    }

    private async ValueTask PutAsync<T>(string relativeUrl, T content)
    {
        HttpResponseMessage response = await this.ariClient.PutAsJsonAsync(relativeUrl, content);
        response.EnsureSuccessStatusCode();
    }
}
