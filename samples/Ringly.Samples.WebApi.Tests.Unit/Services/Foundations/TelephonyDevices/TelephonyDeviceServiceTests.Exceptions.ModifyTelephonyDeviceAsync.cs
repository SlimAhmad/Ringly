using FluentAssertions;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnModifyIfSqlErrorOccursAndLogItAsync()
    {
        // given
        TelephonyDevice telephonyDevice = CreateRandomTelephonyDevice();
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyDeviceByIdAsync(telephonyDevice.Id))
                .ThrowsAsync(sqlException);

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyDeviceService.ModifyTelephonyDeviceAsync(telephonyDevice);

        // then
        TelephonyDeviceDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceDependencyException>(modifyTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyDeviceDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyDeviceByIdAsync(telephonyDevice.Id),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnModifyIfServiceErrorOccursAndLogItAsync()
    {
        // given
        TelephonyDevice telephonyDevice = CreateRandomTelephonyDevice();
        var serviceException = new Exception("service error");

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyDeviceByIdAsync(telephonyDevice.Id))
                .ThrowsAsync(serviceException);

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyDeviceService.ModifyTelephonyDeviceAsync(telephonyDevice);

        // then
        TelephonyDeviceServiceException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceServiceException>(modifyTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyDeviceServiceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyDeviceByIdAsync(telephonyDevice.Id),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
