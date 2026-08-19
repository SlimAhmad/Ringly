using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldAddTelephonyCallAsync()
    {
        // given
        TelephonyCall randomTelephonyCall = CreateRandomTelephonyCall();
        TelephonyCall inputTelephonyCall = randomTelephonyCall.DeepClone();
        TelephonyCall storageTelephonyCall = inputTelephonyCall.DeepClone();
        TelephonyCall expectedTelephonyCall = storageTelephonyCall.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyCallAsync(inputTelephonyCall))
                .ReturnsAsync(storageTelephonyCall);

        // when
        TelephonyCall actualTelephonyCall =
            await this.telephonyCallService.AddTelephonyCallAsync(inputTelephonyCall);

        // then
        actualTelephonyCall.Should().BeEquivalentTo(expectedTelephonyCall);

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyCallAsync(inputTelephonyCall),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
