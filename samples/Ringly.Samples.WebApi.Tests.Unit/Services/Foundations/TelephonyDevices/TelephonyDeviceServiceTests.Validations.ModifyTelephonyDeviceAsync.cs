using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnModifyIfTelephonyDeviceIsNullAndLogItAsync()
    {
        // given
        TelephonyDevice? nullTelephonyDevice = null;

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyDeviceService.ModifyTelephonyDeviceAsync(nullTelephonyDevice!);

        // then
        TelephonyDeviceValidationException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceValidationException>(modifyTask);

        actualException.InnerException.Should().BeOfType<NullTelephonyDeviceException>();

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
    public async Task ShouldThrowValidationExceptionOnModifyIfTelephonyDeviceIsInvalidAndLogItAsync()
    {
        // given
        var invalidTelephonyDevice = new TelephonyDevice();

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyDeviceService.ModifyTelephonyDeviceAsync(invalidTelephonyDevice);

        // then
        TelephonyDeviceValidationException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceValidationException>(modifyTask);

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
    public async Task ShouldThrowValidationExceptionOnModifyIfTelephonyDeviceDoesNotExistAndLogItAsync()
    {
        // given
        TelephonyDevice randomTelephonyDevice = CreateRandomTelephonyDevice();
        TelephonyDevice nonExistentTelephonyDevice = randomTelephonyDevice;
        TelephonyDevice? nullStorageTelephonyDevice = null;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyDeviceByIdAsync(nonExistentTelephonyDevice.Id))
                .ReturnsAsync(nullStorageTelephonyDevice);

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyDeviceService.ModifyTelephonyDeviceAsync(nonExistentTelephonyDevice);

        // then
        TelephonyDeviceValidationException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceValidationException>(modifyTask);

        actualException.InnerException.Should().BeOfType<NotFoundTelephonyDeviceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyDeviceByIdAsync(nonExistentTelephonyDevice.Id),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
