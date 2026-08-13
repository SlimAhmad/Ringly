using FluentAssertions;
using Ringly.Abstractions.Models;

namespace Ringly.CallCenter.Twilio.Tests.Unit.Services.Foundations.Queues;

public partial class TwilioCallCenterProviderTests
{
    [Fact]
    public async Task ShouldStreamNoTransferRequestsAsync()
    {
        // given
        var receivedEvents = new List<ChannelTransferEvent>();
        bool completed = false;

        // when
        IObservable<ChannelTransferEvent> actualStream =
            this.twilioCallCenterProvider.StreamTransferRequests();

        using IDisposable subscription = actualStream.Subscribe(
            onNext: receivedEvents.Add,
            onCompleted: () => completed = true);

        // then
        receivedEvents.Should().BeEmpty();
        completed.Should().BeTrue();

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldSendTransferProgressAsync()
    {
        // given
        string someChannelId = GetRandomString();
        TransferState someState = TransferState.ChannelAnswered;

        // when
        await this.twilioCallCenterProvider.SendTransferProgressAsync(someChannelId, someState);

        // then
        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
