using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldRemoveTelephonyIdentityByIdAsync()
    {
        // given
        TelephonyIdentity randomTelephonyIdentity = CreateRandomTelephonyIdentity();
        Guid inputTelephonyIdentityId = randomTelephonyIdentity.Id;
        TelephonyIdentity storageTelephonyIdentity = randomTelephonyIdentity.DeepClone();
        TelephonyIdentity deletedTelephonyIdentity = storageTelephonyIdentity.DeepClone();
        TelephonyIdentity expectedTelephonyIdentity = deletedTelephonyIdentity.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityByIdAsync(inputTelephonyIdentityId))
                .ReturnsAsync(storageTelephonyIdentity);

        this.storageBrokerMock.Setup(broker =>
            broker.DeleteTelephonyIdentityAsync(storageTelephonyIdentity))
                .ReturnsAsync(deletedTelephonyIdentity);

        // when
        TelephonyIdentity actualTelephonyIdentity =
            await this.telephonyIdentityService.RemoveTelephonyIdentityByIdAsync(inputTelephonyIdentityId);

        // then
        actualTelephonyIdentity.Should().BeEquivalentTo(expectedTelephonyIdentity);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityByIdAsync(inputTelephonyIdentityId),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.DeleteTelephonyIdentityAsync(storageTelephonyIdentity),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
