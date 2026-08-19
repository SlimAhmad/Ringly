using EFxceptions.Models.Exceptions;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnAddIfSqlErrorOccursAndLogItAsync()
    {
        // given
        TelephonyDevice telephonyDevice = CreateRandomTelephonyDevice();
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyDeviceAsync(telephonyDevice))
                .ThrowsAsync(sqlException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyDeviceService.AddTelephonyDeviceAsync(telephonyDevice);

        // then
        TelephonyDeviceDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceDependencyException>(addTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyDeviceDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyDeviceAsync(telephonyDevice),
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
        TelephonyDevice telephonyDevice = CreateRandomTelephonyDevice();
        var duplicateKeyException = new DuplicateKeyException("duplicate key");

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyDeviceAsync(telephonyDevice))
                .ThrowsAsync(duplicateKeyException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyDeviceService.AddTelephonyDeviceAsync(telephonyDevice);

        // then
        TelephonyDeviceDependencyValidationException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceDependencyValidationException>(addTask);

        actualException.InnerException.Should().BeOfType<AlreadyExistsTelephonyDeviceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyDeviceAsync(telephonyDevice),
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
        TelephonyDevice telephonyDevice = CreateRandomTelephonyDevice();
        var foreignKeyConstraintConflictException = new ForeignKeyConstraintConflictException("fk conflict");

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyDeviceAsync(telephonyDevice))
                .ThrowsAsync(foreignKeyConstraintConflictException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyDeviceService.AddTelephonyDeviceAsync(telephonyDevice);

        // then
        TelephonyDeviceDependencyValidationException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceDependencyValidationException>(addTask);

        actualException.InnerException.Should().BeOfType<InvalidReferenceTelephonyDeviceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyDeviceAsync(telephonyDevice),
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
        TelephonyDevice telephonyDevice = CreateRandomTelephonyDevice();
        var dbUpdateException = new DbUpdateException();

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyDeviceAsync(telephonyDevice))
                .ThrowsAsync(dbUpdateException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyDeviceService.AddTelephonyDeviceAsync(telephonyDevice);

        // then
        TelephonyDeviceDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceDependencyException>(addTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyDeviceDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyDeviceAsync(telephonyDevice),
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
        TelephonyDevice telephonyDevice = CreateRandomTelephonyDevice();
        var serviceException = new Exception("service error");

        this.storageBrokerMock.Setup(broker =>
            broker.InsertTelephonyDeviceAsync(telephonyDevice))
                .ThrowsAsync(serviceException);

        // when
        Func<Task> addTask = async () =>
            await this.telephonyDeviceService.AddTelephonyDeviceAsync(telephonyDevice);

        // then
        TelephonyDeviceServiceException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceServiceException>(addTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyDeviceServiceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.InsertTelephonyDeviceAsync(telephonyDevice),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
