using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnRetrieveBySipUsernameIfSipUsernameIsInvalidAndLogItAsync()
    {
        // given
        string invalidSipUsername = string.Empty;

        // when
        Func<Task> retrieveTask = async () =>
            await this.telephonyIdentityService.RetrieveTelephonyIdentityBySipUsernameAsync(invalidSipUsername);

        // then
        TelephonyIdentityValidationException actualException =
            await Assert.ThrowsAsync<TelephonyIdentityValidationException>(retrieveTask);

        actualException.InnerException.Should().BeOfType<InvalidTelephonyIdentityException>();

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(actualException))),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
