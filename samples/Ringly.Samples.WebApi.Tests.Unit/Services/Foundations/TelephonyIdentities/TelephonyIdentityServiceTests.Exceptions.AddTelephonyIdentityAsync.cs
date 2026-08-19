using EFxceptions.Models.Exceptions;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnAddIfSqlErrorOccursAndLogItAsync()
    {
        // given
        TelephonyIdentity telephonyIdentity = CreateRandomTelephonyIdentity();
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyIdentityAsync(telephonyIdentity))
                .ThrowsAsync(sqlException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyIdentityService.AddTelephonyIdentityAsync(telephonyIdentity);

        // then
        TelephonyIdentityDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityDependencyException>(addTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyIdentityDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyIdentityAsync(telephonyIdentity),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnAddIfDuplicateKeyErrorOccursAndLogItAsync()
    {
        // given
        TelephonyIdentity telephonyIdentity = CreateRandomTelephonyIdentity();
        var duplicateKeyException = new DuplicateKeyException("duplicate key");

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyIdentityAsync(telephonyIdentity))
                .ThrowsAsync(duplicateKeyException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyIdentityService.AddTelephonyIdentityAsync(telephonyIdentity);

        // then
        TelephonyIdentityDependencyValidationException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityDependencyValidationException>(addTask);

        actualException.InnerException.Should().BeOfType<AlreadyExistsTelephonyIdentityException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyIdentityAsync(telephonyIdentity),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyExceptionOnAddIfDbUpdateErrorOccursAndLogItAsync()
    {
        // given
        TelephonyIdentity telephonyIdentity = CreateRandomTelephonyIdentity();
        var dbUpdateException = new DbUpdateException();

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyIdentityAsync(telephonyIdentity))
                .ThrowsAsync(dbUpdateException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyIdentityService.AddTelephonyIdentityAsync(telephonyIdentity);

        // then
        TelephonyIdentityDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityDependencyException>(addTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyIdentityDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyIdentityAsync(telephonyIdentity),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnAddIfServiceErrorOccursAndLogItAsync()
    {
        // given
        TelephonyIdentity telephonyIdentity = CreateRandomTelephonyIdentity();
        var serviceException = new Exception("service error");

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyIdentityAsync(telephonyIdentity))
                .ThrowsAsync(serviceException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyIdentityService.AddTelephonyIdentityAsync(telephonyIdentity);

        // then
        TelephonyIdentityServiceException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityServiceException>(addTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyIdentityServiceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyIdentityAsync(telephonyIdentity),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
