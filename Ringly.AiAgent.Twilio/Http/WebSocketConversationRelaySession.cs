using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Ringly.AiAgent.Twilio.Models;

namespace Ringly.AiAgent.Twilio.Http;

// The real IConversationRelaySession implementation — a thin JSON-over-WebSocket sender. Kept
// deliberately dumb (no buffering/backpressure/reconnect logic): ConversationRelay is a
// single-shot per-call connection, not a long-lived stream that needs resilience machinery.
internal sealed class WebSocketConversationRelaySession(WebSocket webSocket) : IConversationRelaySession
{
    public ValueTask SendTextAsync(string text) =>
        SendAsync(new ConversationRelayTextMessage { Token = text, Last = true });

    public ValueTask SendEndSessionAsync(string? handoffData) =>
        SendAsync(new ConversationRelayEndMessage { HandoffData = handoffData });

    private async ValueTask SendAsync(object message)
    {
        string json = JsonConvert.SerializeObject(message);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);
    }
}
