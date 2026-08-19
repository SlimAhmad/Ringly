using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnAddIfTelephonyIdentityIsNullAndLogItAsync()
    {
        // given
        TelephonyIdentity? nullTelephonyIdentity = null;

        // when
        Func<Task> addTask = async () =>
            await this.telephonyIdentityService.AddTelephonyIdentityAsync(nullTelephonyIdentity!);

        // then
        TelephonyIdentityValidationException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityValidationException>(addTask);

        actualException.InnerException.Should().BeOfType<NullTelephonyIdentityException>();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyIdentityAsync(It.IsAny<TelephonyIdentity>()),
                Times.Never);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnAddIfTelephonyIdentityIsInvalidAndLogItAsync()
    {
        // given
        var invalidTelephonyIdentity = new TelephonyIdentity();

        // when
        Func<Task> addTask = async () =>
            await this.telephonyIdentityService.AddTelephonyIdentityAsync(invalidTelephonyIdentity);

        // then
        TelephonyIdentityValidationException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityValidationException>(addTask);

        actualException.InnerException.Should().BeOfType<InvalidTelephonyIdentityException>();
        var invalidException = actualException.InnerException as InvalidTelephonyIdentityException;

        invalidException!.Data.Contains(nameof(TelephonyIdentity.Id)).Should().BeTrue();
        invalidException.Data.Contains(nameof(TelephonyIdentity.UserId)).Should().BeTrue();
        invalidException.Data.Contains(nameof(TelephonyIdentity.SipUsername)).Should().BeTrue();
        invalidException.Data.Contains(nameof(TelephonyIdentity.SipCredential)).Should().BeTrue();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyIdentityAsync(It.IsAny<TelephonyIdentity>()),
                Times.Never);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
