using FluentAssertions;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveByIdIfSqlErrorOccursAndLogItAsync()
    {
        // given
        Guid randomTelephonyCallId = Guid.NewGuid();
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyCallByIdAsync(randomTelephonyCallId))
                .ThrowsAsync(sqlException);

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyCallService.RetrieveTelephonyCallByIdAsync(randomTelephonyCallId);

        // then
        TelephonyCallDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyCallDependencyException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyCallDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyCallByIdAsync(randomTelephonyCallId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRetrieveByIdIfServiceErrorOccursAndLogItAsync()
    {
        // given
        Guid randomTelephonyCallId = Guid.NewGuid();
        var serviceException = new Exception("service error");

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyCallByIdAsync(randomTelephonyCallId))
                .ThrowsAsync(serviceException);

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyCallService.RetrieveTelephonyCallByIdAsync(randomTelephonyCallId);

        // then
        TelephonyCallServiceException actualException =
            await Assert.ThrowsAsync<TelephonyCallServiceException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyCallServiceException>();

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
