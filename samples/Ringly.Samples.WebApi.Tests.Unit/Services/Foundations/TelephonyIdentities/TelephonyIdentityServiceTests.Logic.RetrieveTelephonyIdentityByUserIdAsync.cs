using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldRetrieveTelephonyIdentityByUserIdAsync()
    {
        // given
        TelephonyIdentity randomTelephonyIdentity = CreateRandomTelephonyIdentity();
        Guid inputUserId = randomTelephonyIdentity.UserId;
        TelephonyIdentity storageTelephonyIdentity = randomTelephonyIdentity.DeepClone();
        TelephonyIdentity? expectedTelephonyIdentity = storageTelephonyIdentity.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityByUserIdAsync(inputUserId))
                .ReturnsAsync(storageTelephonyIdentity);

        // when
        TelephonyIdentity? actualTelephonyIdentity =
            await this.telephonyIdentityService.RetrieveTelephonyIdentityByUserIdAsync(inputUserId);

        // then
        actualTelephonyIdentity.Should().BeEquivalentTo(expectedTelephonyIdentity);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityByUserIdAsync(inputUserId),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
