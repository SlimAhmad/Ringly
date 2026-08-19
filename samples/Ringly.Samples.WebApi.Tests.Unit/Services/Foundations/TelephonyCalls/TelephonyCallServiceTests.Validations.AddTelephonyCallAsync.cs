using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnAddIfTelephonyCallIsNullAndLogItAsync()
    {
        // given
        TelephonyCall? nullTelephonyCall = null;

        // when
        Func<Task> addTask = async () =>
            await this.telephonyCallService.AddTelephonyCallAsync(nullTelephonyCall!);

        // then
        TelephonyCallValidationException actualException =
            await Assert.ThrowsAsync<TelephonyCallValidationException>(addTask);

        actualException.InnerException.Should().BeOfType<NullTelephonyCallException>();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyCallAsync(It.IsAny<TelephonyCall>()),
                Times.Never);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnAddIfTelephonyCallIsInvalidAndLogItAsync()
    {
        // given
        var invalidTelephonyCall = new TelephonyCall();

        // when
        Func<Task> addTask = async () =>
            await this.telephonyCallService.AddTelephonyCallAsync(invalidTelephonyCall);

        // then
        TelephonyCallValidationException actualException =
            await Assert.ThrowsAsync<TelephonyCallValidationException>(addTask);

        actualException.InnerException.Should().BeOfType<InvalidTelephonyCallException>();
        var invalidException = actualException.InnerException as InvalidTelephonyCallException;

        invalidException!.Data.Contains(nameof(TelephonyCall.Id)).Should().BeTrue();
        invalidException.Data.Contains(nameof(TelephonyCall.CallerIdentityId)).Should().BeTrue();
        invalidException.Data.Contains(nameof(TelephonyCall.RecipientIdentityId)).Should().BeTrue();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyCallAsync(It.IsAny<TelephonyCall>()),
                Times.Never);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
