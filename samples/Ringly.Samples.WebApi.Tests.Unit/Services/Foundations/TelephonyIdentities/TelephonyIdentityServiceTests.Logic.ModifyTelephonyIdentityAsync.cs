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

        // ModifyTelephonyIdentityAsync copies the input's changes onto the already-tracked
        // instance SelectTelephonyIdentityByIdAsync returned (storageTelephonyIdentity), then
        // updates that same instance — not the caller-supplied inputTelephonyIdentity — to avoid
        // EF Core's "already being tracked" conflict (confirmed live, see
        // RecordingService.ModifyRecordingAsync).
        this.storageBrokerMock.Setup(broker =>
            broker.UpdateTelephonyIdentityAsync(storageTelephonyIdentity))
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
            broker.UpdateTelephonyIdentityAsync(storageTelephonyIdentity),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
