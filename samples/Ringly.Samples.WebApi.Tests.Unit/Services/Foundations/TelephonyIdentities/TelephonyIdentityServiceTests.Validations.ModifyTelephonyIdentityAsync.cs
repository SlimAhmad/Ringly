using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnModifyIfTelephonyIdentityIsNullAndLogItAsync()
    {
        // given
        TelephonyIdentity? nullTelephonyIdentity = null;

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyIdentityService.ModifyTelephonyIdentityAsync(nullTelephonyIdentity!);

        // then
        TelephonyIdentityValidationException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityValidationException>(modifyTask);

        actualException.InnerException.Should().BeOfType<NullTelephonyIdentityException>();

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
    public async Task ShouldThrowValidationExceptionOnModifyIfTelephonyIdentityIsInvalidAndLogItAsync()
    {
        // given
        var invalidTelephonyIdentity = new TelephonyIdentity();

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyIdentityService.ModifyTelephonyIdentityAsync(invalidTelephonyIdentity);

        // then
        TelephonyIdentityValidationException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityValidationException>(modifyTask);

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
    public async Task ShouldThrowValidationExceptionOnModifyIfTelephonyIdentityDoesNotExistAndLogItAsync()
    {
        // given
        TelephonyIdentity randomTelephonyIdentity = CreateRandomTelephonyIdentity();
        TelephonyIdentity nonExistentTelephonyIdentity = randomTelephonyIdentity;
        TelephonyIdentity? nullStorageTelephonyIdentity = null;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityByIdAsync(nonExistentTelephonyIdentity.Id))
                .ReturnsAsync(nullStorageTelephonyIdentity);

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyIdentityService.ModifyTelephonyIdentityAsync(nonExistentTelephonyIdentity);

        // then
        TelephonyIdentityValidationException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityValidationException>(modifyTask);

        actualException.InnerException.Should().BeOfType<NotFoundTelephonyIdentityException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityByIdAsync(nonExistentTelephonyIdentity.Id),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
