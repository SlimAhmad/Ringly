using FluentAssertions;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveBySipUsernameIfSqlErrorOccursAndLogItAsync()
    {
        // given
        string randomSipUsername = Guid.NewGuid().ToString();
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityBySipUsernameAsync(randomSipUsername))
                .ThrowsAsync(sqlException);

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyIdentityService.RetrieveTelephonyIdentityBySipUsernameAsync(randomSipUsername);

        // then
        TelephonyIdentityDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityDependencyException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyIdentityDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityBySipUsernameAsync(randomSipUsername),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRetrieveBySipUsernameIfServiceErrorOccursAndLogItAsync()
    {
        // given
        string randomSipUsername = Guid.NewGuid().ToString();
        var serviceException = new Exception("service error");

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyIdentityBySipUsernameAsync(randomSipUsername))
                .ThrowsAsync(serviceException);

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyIdentityService.RetrieveTelephonyIdentityBySipUsernameAsync(randomSipUsername);

        // then
        TelephonyIdentityServiceException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityServiceException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyIdentityServiceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyIdentityBySipUsernameAsync(randomSipUsername),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
