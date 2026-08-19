using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldRetrieveTelephonyIdentityByIdAsync()
    {
        // given
        TelephonyIdentity randomTelephonyIdentity = CreateRandomTelephonyIdentity();
        Guid inputTelephonyIdentityId = randomTelephonyIdentity.Id;
        TelephonyIdentity storageTelephonyIdentity = randomTelephonyIdentity.DeepClone();
        TelephonyIdentity expectedTelephonyIdentity = storageTelephonyIdentity.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityByIdAsync(inputTelephonyIdentityId))
                .ReturnsAsync(storageTelephonyIdentity);

        // when
        TelephonyIdentity actualTelephonyIdentity =
            await this.telephonyIdentityService.RetrieveTelephonyIdentityByIdAsync(inputTelephonyIdentityId);

        // then
        actualTelephonyIdentity.Should().BeEquivalentTo(expectedTelephonyIdentity);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityByIdAsync(inputTelephonyIdentityId),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
