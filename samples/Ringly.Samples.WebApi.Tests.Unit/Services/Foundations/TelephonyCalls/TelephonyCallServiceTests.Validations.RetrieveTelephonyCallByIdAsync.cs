using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
    {
        // given
        Guid invalidTelephonyCallId = Guid.Empty;

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyCallService.RetrieveTelephonyCallByIdAsync(invalidTelephonyCallId);

        // then
        TelephonyCallValidationException actualException =
            await Assert.ThrowsAsync<TelephonyCallValidationException>(retrieveTask);

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
    public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfTelephonyCallNotFoundAndLogItAsync()
    {
        // given
        Guid randomTelephonyCallId = Guid.NewGuid();
        TelephonyCall? nullStorageTelephonyCall = null;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyCallByIdAsync(randomTelephonyCallId))
                .ReturnsAsync(nullStorageTelephonyCall);

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyCallService.RetrieveTelephonyCallByIdAsync(randomTelephonyCallId);

        // then
        TelephonyCallValidationException actualException =
            await Assert.ThrowsAsync<TelephonyCallValidationException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<NotFoundTelephonyCallException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyCallByIdAsync(randomTelephonyCallId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
