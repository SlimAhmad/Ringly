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

        // ModifyTelephonyCallAsync copies the input's changes onto the already-tracked instance
        // SelectTelephonyCallByIdAsync returned (storageTelephonyCall), then updates that same
        // instance — not the caller-supplied inputTelephonyCall — to avoid EF Core's "already
        // being tracked" conflict (confirmed live, see RecordingService.ModifyRecordingAsync).
        this.storageBrokerMock.Setup(broker =>
            broker.UpdateTelephonyCallAsync(storageTelephonyCall))
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
            broker.UpdateTelephonyCallAsync(storageTelephonyCall),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
