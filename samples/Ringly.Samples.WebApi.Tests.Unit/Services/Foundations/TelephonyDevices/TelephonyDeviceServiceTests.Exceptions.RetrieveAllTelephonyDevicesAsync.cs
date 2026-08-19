using FluentAssertions;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveAllIfSqlErrorOccursAndLogItAsync()
    {
        // given
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectAllTelephonyDevicesAsync())
                .ThrowsAsync(sqlException);

        // when
        Func<Task> retrieveAllTask = async () =>
            await this.telephonyDeviceService.RetrieveAllTelephonyDevicesAsync();

        // then
        TelephonyDeviceDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceDependencyException>(retrieveAllTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyDeviceDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectAllTelephonyDevicesAsync(),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRetrieveAllIfServiceErrorOccursAndLogItAsync()
    {
        // given
        var serviceException = new Exception("service error");

        this.storageBrokerMock.Setup(broker =>
            broker.SelectAllTelephonyDevicesAsync())
                .ThrowsAsync(serviceException);

        // when
        Func<Task> retrieveAllTask = async () =>
            await this.telephonyDeviceService.RetrieveAllTelephonyDevicesAsync();

        // then
        TelephonyDeviceServiceException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceServiceException>(retrieveAllTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyDeviceServiceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectAllTelephonyDevicesAsync(),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
