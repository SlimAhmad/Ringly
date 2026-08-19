using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
    {
        // given
        Guid invalidTelephonyDeviceId = Guid.Empty;

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyDeviceService.RetrieveTelephonyDeviceByIdAsync(invalidTelephonyDeviceId);

        // then
        TelephonyDeviceValidationException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceValidationException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<InvalidTelephonyDeviceException>();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyDeviceByIdAsync(It.IsAny<Guid>()),
                Times.Never);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfTelephonyDeviceNotFoundAndLogItAsync()
    {
        // given
        Guid randomTelephonyDeviceId = Guid.NewGuid();
        TelephonyDevice? nullStorageTelephonyDevice = null;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyDeviceByIdAsync(randomTelephonyDeviceId))
                .ReturnsAsync(nullStorageTelephonyDevice);

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyDeviceService.RetrieveTelephonyDeviceByIdAsync(randomTelephonyDeviceId);

        // then
        TelephonyDeviceValidationException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceValidationException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<NotFoundTelephonyDeviceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyDeviceByIdAsync(randomTelephonyDeviceId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
