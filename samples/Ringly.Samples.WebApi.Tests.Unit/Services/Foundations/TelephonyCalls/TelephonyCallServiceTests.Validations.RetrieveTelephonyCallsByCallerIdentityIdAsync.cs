using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnRetrieveByCallerIdentityIdIfIdIsInvalidAndLogItAsync()
    {
        // given
        Guid invalidCallerIdentityId = Guid.Empty;

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyCallService.RetrieveTelephonyCallsByCallerIdentityIdAsync(invalidCallerIdentityId);

        // then
        TelephonyCallValidationException actualException =
            await Assert.ThrowsAsync<TelephonyCallValidationException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<InvalidTelephonyCallException>();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
