using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldAddTelephonyIdentityAsync()
    {
        // given
        TelephonyIdentity randomTelephonyIdentity = CreateRandomTelephonyIdentity();
        TelephonyIdentity inputTelephonyIdentity = randomTelephonyIdentity.DeepClone();
        TelephonyIdentity storageTelephonyIdentity = inputTelephonyIdentity.DeepClone();
        TelephonyIdentity expectedTelephonyIdentity = storageTelephonyIdentity.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyIdentityAsync(inputTelephonyIdentity))
                .ReturnsAsync(storageTelephonyIdentity);

        // when
        TelephonyIdentity actualTelephonyIdentity =
            await this.telephonyIdentityService.AddTelephonyIdentityAsync(inputTelephonyIdentity);

        // then
        actualTelephonyIdentity.Should().BeEquivalentTo(expectedTelephonyIdentity);

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyIdentityAsync(inputTelephonyIdentity),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
