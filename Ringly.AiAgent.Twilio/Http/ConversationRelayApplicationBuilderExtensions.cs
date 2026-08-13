using Microsoft.AspNetCore.Builder;

namespace Ringly.AiAgent.Twilio.Http;

public static class ConversationRelayApplicationBuilderExtensions
{
    // Consuming app calls this after app.UseWebSockets() in its own pipeline — same
    // "plain class library, host wires it in" pattern as TwilioWebhookController (row #32).
    public static IApplicationBuilder UseTwilioConversationRelay(this IApplicationBuilder app) =>
        app.UseMiddleware<ConversationRelayWebSocketMiddleware>();
}
