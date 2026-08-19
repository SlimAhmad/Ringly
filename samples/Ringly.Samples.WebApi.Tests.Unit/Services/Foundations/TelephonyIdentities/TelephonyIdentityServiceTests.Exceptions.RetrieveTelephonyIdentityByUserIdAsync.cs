using FluentAssertions;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveByUserIdIfSqlErrorOccursAndLogItAsync()
    {
        // given
        Guid randomUserId = Guid.NewGuid();
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityByUserIdAsync(randomUserId))
                .ThrowsAsync(sqlException);

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyIdentityService.RetrieveTelephonyIdentityByUserIdAsync(randomUserId);

        // then
        TelephonyIdentityDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityDependencyException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyIdentityDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityByUserIdAsync(randomUserId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRetrieveByUserIdIfServiceErrorOccursAndLogItAsync()
    {
        // given
        Guid randomUserId = Guid.NewGuid();
        var serviceException = new Exception("service error");

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityByUserIdAsync(randomUserId))
                .ThrowsAsync(serviceException);

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyIdentityService.RetrieveTelephonyIdentityByUserIdAsync(randomUserId);

        // then
        TelephonyIdentityServiceException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityServiceException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyIdentityServiceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityByUserIdAsync(randomUserId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
