using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldAddTelephonyDeviceAsync()
    {
        // given
        TelephonyDevice randomTelephonyDevice = CreateRandomTelephonyDevice();
        TelephonyDevice inputTelephonyDevice = randomTelephonyDevice.DeepClone();
        TelephonyDevice storageTelephonyDevice = inputTelephonyDevice.DeepClone();
        TelephonyDevice expectedTelephonyDevice = storageTelephonyDevice.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyDeviceAsync(inputTelephonyDevice))
                .ReturnsAsync(storageTelephonyDevice);

        // when
        TelephonyDevice actualTelephonyDevice =
            await this.telephonyDeviceService.AddTelephonyDeviceAsync(inputTelephonyDevice);

        // then
        actualTelephonyDevice.Should().BeEquivalentTo(expectedTelephonyDevice);

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyDeviceAsync(inputTelephonyDevice),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
