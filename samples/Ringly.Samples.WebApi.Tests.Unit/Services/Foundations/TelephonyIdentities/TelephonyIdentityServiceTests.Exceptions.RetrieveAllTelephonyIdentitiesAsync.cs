using FluentAssertions;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveAllIfSqlErrorOccursAndLogItAsync()
    {
        // given
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectAllTelephonyIdentitiesAsync())
                .ThrowsAsync(sqlException);

        // when
        Func<Task> retrieveAllTask = async () =>
            await this.telephonyIdentityService.RetrieveAllTelephonyIdentitiesAsync();

        // then
        TelephonyIdentityDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityDependencyException>(retrieveAllTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyIdentityDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectAllTelephonyIdentitiesAsync(),
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
            broker.SelectAllTelephonyIdentitiesAsync())
                .ThrowsAsync(serviceException);

        // when
        Func<Task> retrieveAllTask = async () =>
            await this.telephonyIdentityService.RetrieveAllTelephonyIdentitiesAsync();

        // then
        TelephonyIdentityServiceException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityServiceException>(retrieveAllTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyIdentityServiceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectAllTelephonyIdentitiesAsync(),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
