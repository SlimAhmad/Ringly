using EFxceptions.Models.Exceptions;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnAddIfSqlErrorOccursAndLogItAsync()
    {
        // given
        TelephonyCall telephonyCall = CreateRandomTelephonyCall();
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyCallAsync(telephonyCall))
                .ThrowsAsync(sqlException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyCallService.AddTelephonyCallAsync(telephonyCall);

        // then
        TelephonyCallDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyCallDependencyException>(addTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyCallDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyCallAsync(telephonyCall),
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
        TelephonyCall telephonyCall = CreateRandomTelephonyCall();
        var duplicateKeyException = new DuplicateKeyException("duplicate key");

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyCallAsync(telephonyCall))
                .ThrowsAsync(duplicateKeyException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyCallService.AddTelephonyCallAsync(telephonyCall);

        // then
        TelephonyCallDependencyValidationException actualException =
            await Assert.ThrowsAsync<TelephonyCallDependencyValidationException>(addTask);

        actualException.InnerException.Should().BeOfType<AlreadyExistsTelephonyCallException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyCallAsync(telephonyCall),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnAddIfForeignKeyConstraintConflictErrorOccursAndLogItAsync()
    {
        // given
        TelephonyCall telephonyCall = CreateRandomTelephonyCall();
        var foreignKeyConstraintConflictException = new ForeignKeyConstraintConflictException("fk conflict");

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyCallAsync(telephonyCall))
                .ThrowsAsync(foreignKeyConstraintConflictException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyCallService.AddTelephonyCallAsync(telephonyCall);

        // then
        TelephonyCallDependencyValidationException actualException =
            await Assert.ThrowsAsync<TelephonyCallDependencyValidationException>(addTask);

        actualException.InnerException.Should().BeOfType<InvalidReferenceTelephonyCallException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyCallAsync(telephonyCall),
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
        TelephonyCall telephonyCall = CreateRandomTelephonyCall();
        var dbUpdateException = new DbUpdateException();

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyCallAsync(telephonyCall))
                .ThrowsAsync(dbUpdateException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyCallService.AddTelephonyCallAsync(telephonyCall);

        // then
        TelephonyCallDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyCallDependencyException>(addTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyCallDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyCallAsync(telephonyCall),
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
        TelephonyCall telephonyCall = CreateRandomTelephonyCall();
        var serviceException = new Exception("service error");

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyCallAsync(telephonyCall))
                .ThrowsAsync(serviceException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyCallService.AddTelephonyCallAsync(telephonyCall);

        // then
        TelephonyCallServiceException actualException =
            await Assert.ThrowsAsync<TelephonyCallServiceException>(addTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyCallServiceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyCallAsync(telephonyCall),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
