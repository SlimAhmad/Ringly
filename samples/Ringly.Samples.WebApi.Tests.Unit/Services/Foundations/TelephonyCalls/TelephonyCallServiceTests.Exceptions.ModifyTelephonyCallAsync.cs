using FluentAssertions;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnModifyIfSqlErrorOccursAndLogItAsync()
    {
        // given
        TelephonyCall telephonyCall = CreateRandomTelephonyCall();
        SqlException sqlException = CreateSqlException();

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyCallByIdAsync(telephonyCall.Id))
                .ThrowsAsync(sqlException);

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyCallService.ModifyTelephonyCallAsync(telephonyCall);

        // then
        TelephonyCallDependencyException actualException =
            await Assert.ThrowsAsync<TelephonyCallDependencyException>(modifyTask);

        actualException.InnerException.Should().BeOfType<FailedStorageTelephonyCallDependencyException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyCallByIdAsync(telephonyCall.Id),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnModifyIfServiceErrorOccursAndLogItAsync()
    {
        // given
        TelephonyCall telephonyCall = CreateRandomTelephonyCall();
        var serviceException = new Exception("service error");

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyCallByIdAsync(telephonyCall.Id))
                .ThrowsAsync(serviceException);

        // when
        Func<Task> modifyTask = async () =>
            await this.telephonyCallService.ModifyTelephonyCallAsync(telephonyCall);

        // then
        TelephonyCallServiceException actualException =
            await Assert.ThrowsAsync<TelephonyCallServiceException>(modifyTask);

        actualException.InnerException.Should().BeOfType<FailedTelephonyCallServiceException>();

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyCallByIdAsync(telephonyCall.Id),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
