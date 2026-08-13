using System.Security;

namespace Ringly.AiAgent.Twilio;

// <Connect><ConversationRelay url="..." welcomeGreeting="..." voice="..."/></Connect> — attribute
// names confirmed against Twilio's <ConversationRelay> TwiML noun reference
// (twilio.com/docs/voice/twiml/connect/conversationrelay), not guessed. AiAgentConfig.SystemPrompt
// has no ConversationRelay TwiML equivalent — it's opaque to Twilio, consumed entirely by the
// app's own IAiAgentResponder implementation, so it's never placed in this markup.
internal static class ConversationRelayTwimlBuilder
{
    public static string Build(string webSocketUrl, string welcomeGreeting, string ttsVoice)
    {
        string voiceAttribute = OptionalAttribute("voice", ttsVoice);
        string welcomeGreetingAttribute = OptionalAttribute("welcomeGreeting", welcomeGreeting);

        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Response><Connect><ConversationRelay" +
            $" url=\"{SecurityElement.Escape(webSocketUrl)}\"" +
            welcomeGreetingAttribute +
            voiceAttribute +
            "/></Connect></Response>";
    }

    private static string OptionalAttribute(string name, string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : $" {name}=\"{SecurityElement.Escape(value)}\"";
}
