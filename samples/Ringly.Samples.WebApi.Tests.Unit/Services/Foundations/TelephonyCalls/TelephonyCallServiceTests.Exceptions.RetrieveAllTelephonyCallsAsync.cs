using FluentAssertions;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveAllIfSqlErrorOccursAndLogItAsync()
    {
        // given
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectAllTelephonyCallsAsync())
                .ThrowsAsync(sqlException);

        // when
        Func<Task> retrieveAllTask = async () =>
            await this.telephonyCallService.RetrieveAllTelephonyCallsAsync();

        // then
        TelephonyCallDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyCallDependencyException>(retrieveAllTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyCallDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectAllTelephonyCallsAsync(),
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
            broker.SelectAllTelephonyCallsAsync())
                .ThrowsAsync(serviceException);

        // when
        Func<Task> retrieveAllTask = async () =>
            await this.telephonyCallService.RetrieveAllTelephonyCallsAsync();

        // then
        TelephonyCallServiceException actualException =
            await Assert.ThrowsAsync<TelephonyCallServiceException>(retrieveAllTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyCallServiceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectAllTelephonyCallsAsync(),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
