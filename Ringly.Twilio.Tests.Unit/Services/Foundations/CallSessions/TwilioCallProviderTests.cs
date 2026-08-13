using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Brokers;
using Ringly.Twilio.Services.Foundations.CallSessions;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
{
    private const string DefaultCallerId = "+15551234567";

    private readonly Mock<ITwilioBroker> twilioBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly TwilioCallProvider twilioCallProvider;

    public TwilioCallProviderTests()
    {
        this.twilioBrokerMock = new Mock<ITwilioBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        var options = Options.Create(new TwilioOptions { DefaultCallerId = DefaultCallerId });

        this.twilioCallProvider = new TwilioCallProvider(
            twilioBroker: this.twilioBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object,
            twilioOptions: options);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static string GetRandomString() =>
        new MnemonicString(wordCount: 3).GetValue();

    private static CallParticipant CreateRandomCallParticipant() =>
        CreateCallParticipantFiller().Create();

    private static Filler<CallParticipant> CreateCallParticipantFiller()
    {
        var filler = new Filler<CallParticipant>();
        filler.Setup();

        return filler;
    }
}
