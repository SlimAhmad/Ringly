using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnAddIfTelephonyDeviceIsNullAndLogItAsync()
    {
        // given
        TelephonyDevice? nullTelephonyDevice = null;

        // when
        Func<Task> addTask = async () =>
            await this.telephonyDeviceService.AddTelephonyDeviceAsync(nullTelephonyDevice!);

        // then
        TelephonyDeviceValidationException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceValidationException>(addTask);

        actualException.InnerException.Should().BeOfType<NullTelephonyDeviceException>();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyDeviceAsync(It.IsAny<TelephonyDevice>()),
                Times.Never);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnAddIfTelephonyDeviceIsInvalidAndLogItAsync()
    {
        // given
        var invalidTelephonyDevice = new TelephonyDevice();

        // when
        Func<Task> addTask = async () =>
            await this.telephonyDeviceService.AddTelephonyDeviceAsync(invalidTelephonyDevice);

        // then
        TelephonyDeviceValidationException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceValidationException>(addTask);

        actualException.InnerException.Should().BeOfType<InvalidTelephonyDeviceException>();
        var invalidException = actualException.InnerException as InvalidTelephonyDeviceException;

        invalidException!.Data.Contains(nameof(TelephonyDevice.Id)).Should().BeTrue();
        invalidException.Data.Contains(nameof(TelephonyDevice.IdentityId)).Should().BeTrue();
        invalidException.Data.Contains(nameof(TelephonyDevice.Platform)).Should().BeTrue();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyDeviceAsync(It.IsAny<TelephonyDevice>()),
                Times.Never);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
