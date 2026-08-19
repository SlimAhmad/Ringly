using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldRemoveTelephonyDeviceByIdAsync()
    {
        // given
        TelephonyDevice randomTelephonyDevice = CreateRandomTelephonyDevice();
        Guid inputTelephonyDeviceId = randomTelephonyDevice.Id;
        TelephonyDevice storageTelephonyDevice = randomTelephonyDevice.DeepClone();
        TelephonyDevice deletedTelephonyDevice = storageTelephonyDevice.DeepClone();
        TelephonyDevice expectedTelephonyDevice = deletedTelephonyDevice.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyDeviceByIdAsync(inputTelephonyDeviceId))
                .ReturnsAsync(storageTelephonyDevice);

        this.storageBrokerMock.Setup(broker =>
            broker.DeleteTelephonyDeviceAsync(storageTelephonyDevice))
                .ReturnsAsync(deletedTelephonyDevice);

        // when
        TelephonyDevice actualTelephonyDevice =
            await this.telephonyDeviceService.RemoveTelephonyDeviceByIdAsync(inputTelephonyDeviceId);

        // then
        actualTelephonyDevice.Should().BeEquivalentTo(expectedTelephonyDevice);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyDeviceByIdAsync(inputTelephonyDeviceId),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.DeleteTelephonyDeviceAsync(storageTelephonyDevice),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
