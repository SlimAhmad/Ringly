using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using Moq;
using Ringly.AiAgent.Abstractions.Models;
using Ringly.AiAgent.Twilio.Services;
using Ringly.Twilio.Brokers;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.AiAgent.Twilio.Tests.Unit.Services;

public partial class TwilioConversationRelayProviderTests
{
    private const string WebSocketBaseUrl = "wss://example.com/conversationrelay";

    private readonly Mock<ITwilioBroker> twilioBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly Mock<IAiAgentResponder> aiAgentResponderMock;
    private readonly TwilioConversationRelayProvider provider;

    public TwilioConversationRelayProviderTests()
    {
        this.twilioBrokerMock = new Mock<ITwilioBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();
        this.aiAgentResponderMock = new Mock<IAiAgentResponder>();

        var options = Options.Create(new ConversationRelayOptions { WebSocketBaseUrl = WebSocketBaseUrl });

        this.provider = new TwilioConversationRelayProvider(
            twilioBroker: this.twilioBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object,
            aiAgentResponder: this.aiAgentResponderMock.Object,
            options: options);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static string GetRandomString() =>
        new MnemonicString(wordCount: 3).GetValue();

    private static AiAgentConfig CreateRandomAiAgentConfig() =>
        CreateAiAgentConfigFiller().Create();

    private static Filler<AiAgentConfig> CreateAiAgentConfigFiller()
    {
        var filler = new Filler<AiAgentConfig>();
        filler.Setup();

        return filler;
    }
}
