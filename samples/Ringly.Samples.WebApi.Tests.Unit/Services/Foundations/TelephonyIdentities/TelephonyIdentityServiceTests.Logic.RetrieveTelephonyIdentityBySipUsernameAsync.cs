using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldRetrieveTelephonyIdentityBySipUsernameAsync()
    {
        // given
        TelephonyIdentity randomTelephonyIdentity = CreateRandomTelephonyIdentity();
        string inputSipUsername = randomTelephonyIdentity.SipUsername;
        TelephonyIdentity storageTelephonyIdentity = randomTelephonyIdentity.DeepClone();
        TelephonyIdentity? expectedTelephonyIdentity = storageTelephonyIdentity.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityBySipUsernameAsync(inputSipUsername))
                .ReturnsAsync(storageTelephonyIdentity);

        // when
        TelephonyIdentity? actualTelephonyIdentity =
            await this.telephonyIdentityService.RetrieveTelephonyIdentityBySipUsernameAsync(inputSipUsername);

        // then
        actualTelephonyIdentity.Should().BeEquivalentTo(expectedTelephonyIdentity);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityBySipUsernameAsync(inputSipUsername),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
