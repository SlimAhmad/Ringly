using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldRemoveTelephonyCallByIdAsync()
    {
        // given
        TelephonyCall randomTelephonyCall = CreateRandomTelephonyCall();
        Guid inputTelephonyCallId = randomTelephonyCall.Id;
        TelephonyCall storageTelephonyCall = randomTelephonyCall.DeepClone();
        TelephonyCall deletedTelephonyCall = storageTelephonyCall.DeepClone();
        TelephonyCall expectedTelephonyCall = deletedTelephonyCall.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyCallByIdAsync(inputTelephonyCallId))
                .ReturnsAsync(storageTelephonyCall);

        this.storageBrokerMock.Setup(broker =>
            broker.DeleteTelephonyCallAsync(storageTelephonyCall))
                .ReturnsAsync(deletedTelephonyCall);

        // when
        TelephonyCall actualTelephonyCall =
            await this.telephonyCallService.RemoveTelephonyCallByIdAsync(inputTelephonyCallId);

        // then
        actualTelephonyCall.Should().BeEquivalentTo(expectedTelephonyCall);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyCallByIdAsync(inputTelephonyCallId),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.DeleteTelephonyCallAsync(storageTelephonyCall),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
