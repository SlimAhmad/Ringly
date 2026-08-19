using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnRemoveByIdIfIdIsInvalidAndLogItAsync()
    {
        // given
        Guid invalidTelephonyIdentityId = Guid.Empty;

        // when
        Func<Task> removeTask = async () =>
            await this.telephonyIdentityService.RemoveTelephonyIdentityByIdAsync(invalidTelephonyIdentityId);

        // then
        TelephonyIdentityValidationException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityValidationException>(removeTask);

        actualException.InnerException.Should().BeOfType<InvalidTelephonyIdentityException>();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityByIdAsync(It.IsAny<Guid>()),
                Times.Never);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnRemoveByIdIfTelephonyIdentityNotFoundAndLogItAsync()
    {
        // given
        Guid randomTelephonyIdentityId = Guid.NewGuid();
        TelephonyIdentity? nullStorageTelephonyIdentity = null;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityByIdAsync(randomTelephonyIdentityId))
                .ReturnsAsync(nullStorageTelephonyIdentity);

        // when
        Func<Task> removeTask = async () =>
            await this.telephonyIdentityService.RemoveTelephonyIdentityByIdAsync(randomTelephonyIdentityId);

        // then
        TelephonyIdentityValidationException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityValidationException>(removeTask);

        actualException.InnerException.Should().BeOfType<NotFoundTelephonyIdentityException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityByIdAsync(randomTelephonyIdentityId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
