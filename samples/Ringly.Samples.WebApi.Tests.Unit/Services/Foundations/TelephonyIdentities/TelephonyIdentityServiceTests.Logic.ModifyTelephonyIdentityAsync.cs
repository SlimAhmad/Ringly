using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldModifyTelephonyIdentityAsync()
    {
        // given
        TelephonyIdentity randomTelephonyIdentity = CreateRandomTelephonyIdentity();
        TelephonyIdentity inputTelephonyIdentity = randomTelephonyIdentity.DeepClone();
        TelephonyIdentity storageTelephonyIdentity = inputTelephonyIdentity.DeepClone();
        TelephonyIdentity updatedTelephonyIdentity = inputTelephonyIdentity.DeepClone();
        TelephonyIdentity expectedTelephonyIdentity = updatedTelephonyIdentity.DeepClone();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityByIdAsync(inputTelephonyIdentity.Id))
                .ReturnsAsync(storageTelephonyIdentity);

        this.storageBrokerMock.Setup(broker =>
            broker.UpdateTelephonyIdentityAsync(inputTelephonyIdentity))
                .ReturnsAsync(updatedTelephonyIdentity);

        // when
        TelephonyIdentity actualTelephonyIdentity =
            await this.telephonyIdentityService.ModifyTelephonyIdentityAsync(inputTelephonyIdentity);

        // then
        actualTelephonyIdentity.Should().BeEquivalentTo(expectedTelephonyIdentity);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityByIdAsync(inputTelephonyIdentity.Id),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.UpdateTelephonyIdentityAsync(inputTelephonyIdentity),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
