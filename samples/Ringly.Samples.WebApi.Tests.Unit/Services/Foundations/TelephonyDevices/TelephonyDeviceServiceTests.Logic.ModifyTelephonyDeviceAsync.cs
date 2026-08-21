using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldModifyTelephonyDeviceAsync()
    {
        // given
        TelephonyDevice randomTelephonyDevice = CreateRandomTelephonyDevice();
        TelephonyDevice inputTelephonyDevice = randomTelephonyDevice.DeepClone();
        TelephonyDevice storageTelephonyDevice = inputTelephonyDevice.DeepClone();
        TelephonyDevice updatedTelephonyDevice = inputTelephonyDevice.DeepClone();
        TelephonyDevice expectedTelephonyDevice = updatedTelephonyDevice.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyDeviceByIdAsync(inputTelephonyDevice.Id))
                .ReturnsAsync(storageTelephonyDevice);

        // ModifyTelephonyDeviceAsync copies the input's changes onto the already-tracked instance
        // SelectTelephonyDeviceByIdAsync returned (storageTelephonyDevice), then updates that
        // same instance — not the caller-supplied inputTelephonyDevice — to avoid EF Core's
        // "already being tracked" conflict (confirmed live, see
        // RecordingService.ModifyRecordingAsync).
        this.storageBrokerMock.Setup(broker =>
            broker.UpdateTelephonyDeviceAsync(storageTelephonyDevice))
                .ReturnsAsync(updatedTelephonyDevice);

        // when
        TelephonyDevice actualTelephonyDevice =
            await this.telephonyDeviceService.ModifyTelephonyDeviceAsync(inputTelephonyDevice);

        // then
        actualTelephonyDevice.Should().BeEquivalentTo(expectedTelephonyDevice);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyDeviceByIdAsync(inputTelephonyDevice.Id),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.UpdateTelephonyDeviceAsync(storageTelephonyDevice),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
