using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldRetrieveAllTelephonyDevicesAsync()
    {
        // given
        IQueryable<TelephonyDevice> storageTelephonyDevices = CreateRandomTelephonyDevices();
        IQueryable<TelephonyDevice> expectedTelephonyDevices = storageTelephonyDevices;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectAllTelephonyDevicesAsync())
                .ReturnsAsync(storageTelephonyDevices);

        // when
        IQueryable<TelephonyDevice> actualTelephonyDevices =
            await this.telephonyDeviceService.RetrieveAllTelephonyDevicesAsync();

        // then
        actualTelephonyDevices.Should().BeEquivalentTo(expectedTelephonyDevices);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectAllTelephonyDevicesAsync(),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
