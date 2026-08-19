using FluentAssertions;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveByIdentityIdIfSqlErrorOccursAndLogItAsync()
    {
        // given
        Guid randomIdentityId = Guid.NewGuid();
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyDevicesByIdentityIdAsync(randomIdentityId))
                .ThrowsAsync(sqlException);

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyDeviceService.RetrieveTelephonyDevicesByIdentityIdAsync(randomIdentityId);

        // then
        TelephonyDeviceDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceDependencyException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyDeviceDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyDevicesByIdentityIdAsync(randomIdentityId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRetrieveByIdentityIdIfServiceErrorOccursAndLogItAsync()
    {
        // given
        Guid randomIdentityId = Guid.NewGuid();
        var serviceException = new Exception("service error");

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyDevicesByIdentityIdAsync(randomIdentityId))
                .ThrowsAsync(serviceException);

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyDeviceService.RetrieveTelephonyDevicesByIdentityIdAsync(randomIdentityId);

        // then
        TelephonyDeviceServiceException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceServiceException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyDeviceServiceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyDevicesByIdentityIdAsync(randomIdentityId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
