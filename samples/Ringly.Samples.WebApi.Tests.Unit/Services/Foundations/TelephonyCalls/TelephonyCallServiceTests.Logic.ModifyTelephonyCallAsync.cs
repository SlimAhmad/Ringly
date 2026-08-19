using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldModifyTelephonyCallAsync()
    {
        // given
        TelephonyCall randomTelephonyCall = CreateRandomTelephonyCall();
        TelephonyCall inputTelephonyCall = randomTelephonyCall.DeepClone();
        TelephonyCall storageTelephonyCall = inputTelephonyCall.DeepClone();
        TelephonyCall updatedTelephonyCall = inputTelephonyCall.DeepClone();
        TelephonyCall expectedTelephonyCall = updatedTelephonyCall.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyCallByIdAsync(inputTelephonyCall.Id))
                .ReturnsAsync(storageTelephonyCall);

        this.storageBrokerMock.Setup(broker =>
            broker.UpdateTelephonyCallAsync(inputTelephonyCall))
                .ReturnsAsync(updatedTelephonyCall);

        // when
        TelephonyCall actualTelephonyCall =
            await this.telephonyCallService.ModifyTelephonyCallAsync(inputTelephonyCall);

        // then
        actualTelephonyCall.Should().BeEquivalentTo(expectedTelephonyCall);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyCallByIdAsync(inputTelephonyCall.Id),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.UpdateTelephonyCallAsync(inputTelephonyCall),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
