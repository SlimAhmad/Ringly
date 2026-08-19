using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnRetrieveByIdentityIdIfIdentityIdIsInvalidAndLogItAsync()
    {
        // given
        Guid invalidIdentityId = Guid.Empty;

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyDeviceService.RetrieveTelephonyDevicesByIdentityIdAsync(invalidIdentityId);

        // then
        TelephonyDeviceValidationException actualException =
            await Assert.ThrowsAsync<TelephonyDeviceValidationException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<InvalidTelephonyDeviceException>();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
