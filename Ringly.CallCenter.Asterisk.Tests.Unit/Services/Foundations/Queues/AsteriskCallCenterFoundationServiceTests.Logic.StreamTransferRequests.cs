using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public void ShouldStreamTransferRequests()
    {
        // given
        var expectedStream = new System.Reactive.Subjects.Subject<ChannelTransferEvent>();

        this.asteriskBrokerMock.Setup(broker =>
            broker.StreamTransferRequests())
                .Returns(expectedStream);

        // when
        IObservable<ChannelTransferEvent> actualStream =
            this.asteriskCallCenterFoundationService.StreamTransferRequests();

        // then
        actualStream.Should().BeSameAs(expectedStream);

        this.asteriskBrokerMock.Verify(broker =>
            broker.StreamTransferRequests(),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
