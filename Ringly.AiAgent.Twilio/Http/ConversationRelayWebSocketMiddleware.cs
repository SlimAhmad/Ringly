using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ringly.AiAgent.Twilio.Models;
using Ringly.AiAgent.Twilio.Services;

namespace Ringly.AiAgent.Twilio.Http;

// Raw WebSocket accept/read-loop plumbing — untested, same convention as row #31's brokers
// (AsteriskBroker's ARI WebSocket loop isn't unit-tested either; the testable logic it drives,
// TwilioConversationRelayProvider.HandleInboundMessageAsync, is). Matches requests under
// PathPrefix, treats the final path segment as the aiSessionId StartAiSessionAsync generated and
// embedded in the wss:// URL handed to Twilio, and otherwise just shuttles JSON frames in and out.
public class ConversationRelayWebSocketMiddleware
{
    public const string PathPrefix = "/conversationrelay/";

    private readonly RequestDelegate next;
    private readonly TwilioConversationRelayProvider provider;

    public ConversationRelayWebSocketMiddleware(RequestDelegate next, TwilioConversationRelayProvider provider)
    {
        this.next = next;
        this.provider = provider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(PathPrefix.TrimEnd('/')) ||
            !context.WebSockets.IsWebSocketRequest ||
            !Guid.TryParse(context.Request.Path.Value?.Split('/').LastOrDefault(), out Guid aiSessionId))
        {
            await this.next(context);
            return;
        }

        using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
        this.provider.RegisterSession(aiSessionId, new WebSocketConversationRelaySession(webSocket));

        try
        {
            await ReadLoopAsync(webSocket, aiSessionId);
        }
        finally
        {
            this.provider.UnregisterSession(aiSessionId);
        }
    }

    private async Task ReadLoopAsync(WebSocket webSocket, Guid aiSessionId)
    {
        var buffer = new byte[8192];

        while (webSocket.State == WebSocketState.Open)
        {
            using var messageStream = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    return;
                }

                messageStream.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            string json = Encoding.UTF8.GetString(messageStream.ToArray());
            var message = JsonConvert.DeserializeObject<ConversationRelayInboundMessage>(json);

            if (message is not null)
            {
                await this.provider.HandleInboundMessageAsync(aiSessionId, message);
            }
        }
    }
}
