using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Processings.Provisioning;

public partial class CallProvisioningServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnAddIfClientIdIsInvalidAndLogItAsync()
    {
        // given
        Guid invalidClientId = Guid.Empty;

        var invalidSipCredentialsException = new InvalidSipCredentialsException();

        invalidSipCredentialsException.UpsertDataList(
            key: "clientId",
            value: "Value is required");

        var expectedValidationException =
            new SipCredentialsValidationException(invalidSipCredentialsException);

        // when
        ValueTask<SipCredentials> addTask =
            this.callProvisioningService.AddClientCredentialsAsync(invalidClientId);

        SipCredentialsValidationException actualException =
            await Assert.ThrowsAsync<SipCredentialsValidationException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
