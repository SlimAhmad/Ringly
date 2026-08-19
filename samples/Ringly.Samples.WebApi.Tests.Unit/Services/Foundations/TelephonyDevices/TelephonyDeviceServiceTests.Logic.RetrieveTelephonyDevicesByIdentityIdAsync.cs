using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldRetrieveTelephonyDevicesByIdentityIdAsync()
    {
        // given
        Guid randomIdentityId = Guid.NewGuid();
        IQueryable<TelephonyDevice> storageTelephonyDevices = CreateRandomTelephonyDevices();
        IQueryable<TelephonyDevice> expectedTelephonyDevices = storageTelephonyDevices;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyDevicesByIdentityIdAsync(randomIdentityId))
                .ReturnsAsync(storageTelephonyDevices);

        // when
        IQueryable<TelephonyDevice> actualTelephonyDevices =
            await this.telephonyDeviceService.RetrieveTelephonyDevicesByIdentityIdAsync(randomIdentityId);

        // then
        actualTelephonyDevices.Should().BeEquivalentTo(expectedTelephonyDevices);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyDevicesByIdentityIdAsync(randomIdentityId),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
