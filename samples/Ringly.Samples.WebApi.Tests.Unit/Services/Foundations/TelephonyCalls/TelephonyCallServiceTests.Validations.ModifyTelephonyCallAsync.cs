using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnModifyIfTelephonyCallIsNullAndLogItAsync()
    {
        // given
        TelephonyCall? nullTelephonyCall = null;

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyCallService.ModifyTelephonyCallAsync(nullTelephonyCall!);

        // then
        TelephonyCallValidationException actualException =
            await Assert.ThrowsAsync<TelephonyCallValidationException>(modifyTask);

        actualException.InnerException.Should().BeOfType<NullTelephonyCallException>();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyCallByIdAsync(It.IsAny<Guid>()),
                Times.Never);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnModifyIfTelephonyCallIsInvalidAndLogItAsync()
    {
        // given
        var invalidTelephonyCall = new TelephonyCall();

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyCallService.ModifyTelephonyCallAsync(invalidTelephonyCall);

        // then
        TelephonyCallValidationException actualException =
            await Assert.ThrowsAsync<TelephonyCallValidationException>(modifyTask);

        actualException.InnerException.Should().BeOfType<InvalidTelephonyCallException>();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyCallByIdAsync(It.IsAny<Guid>()),
                Times.Never);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnModifyIfTelephonyCallDoesNotExistAndLogItAsync()
    {
        // given
        TelephonyCall randomTelephonyCall = CreateRandomTelephonyCall();
        TelephonyCall nonExistentTelephonyCall = randomTelephonyCall;
        TelephonyCall? nullStorageTelephonyCall = null;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyCallByIdAsync(nonExistentTelephonyCall.Id))
                .ReturnsAsync(nullStorageTelephonyCall);

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyCallService.ModifyTelephonyCallAsync(nonExistentTelephonyCall);

        // then
        TelephonyCallValidationException actualException =
            await Assert.ThrowsAsync<TelephonyCallValidationException>(modifyTask);

        actualException.InnerException.Should().BeOfType<NotFoundTelephonyCallException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyCallByIdAsync(nonExistentTelephonyCall.Id),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
