using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public void ShouldStreamCallBroadcasts()
    {
        // given
        var expectedStream = new System.Reactive.Subjects.Subject<CallBroadcastEvent>();

        this.asteriskBrokerMock.Setup(broker =>
            broker.StreamCallBroadcasts())
                .Returns(expectedStream);

        // when
        IObservable<CallBroadcastEvent> actualStream =
            this.asteriskCallCenterFoundationService.StreamCallBroadcasts();

        // then
        actualStream.Should().BeSameAs(expectedStream);

        this.asteriskBrokerMock.Verify(broker =>
            broker.StreamCallBroadcasts(),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
