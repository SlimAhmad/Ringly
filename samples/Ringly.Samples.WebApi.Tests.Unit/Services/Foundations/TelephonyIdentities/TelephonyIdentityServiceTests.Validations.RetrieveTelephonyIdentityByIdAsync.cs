using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
    {
        // given
        Guid invalidTelephonyIdentityId = Guid.Empty;

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyIdentityService.RetrieveTelephonyIdentityByIdAsync(invalidTelephonyIdentityId);

        // then
        TelephonyIdentityValidationException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityValidationException>(retrieveTask);

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
    public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfTelephonyIdentityNotFoundAndLogItAsync()
    {
        // given
        Guid randomTelephonyIdentityId = Guid.NewGuid();
        TelephonyIdentity? nullStorageTelephonyIdentity = null;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityByIdAsync(randomTelephonyIdentityId))
                .ReturnsAsync(nullStorageTelephonyIdentity);

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyIdentityService.RetrieveTelephonyIdentityByIdAsync(randomTelephonyIdentityId);

        // then
        TelephonyIdentityValidationException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityValidationException>(retrieveTask);

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
