using FluentAssertions;
using Moq;
using Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Processings.Provisioning;

public partial class CallProvisioningServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ShouldThrowValidationExceptionOnRemoveIfExtensionIsInvalidAndLogItAsync(
        string? invalidExtension)
    {
        // given
        var invalidSipCredentialsException = new InvalidSipCredentialsException();

        invalidSipCredentialsException.UpsertDataList(
            key: "extension",
            value: "Value is required");

        var expectedValidationException =
            new SipCredentialsValidationException(invalidSipCredentialsException);

        // when
        ValueTask removeTask =
            this.callProvisioningService.RemoveClientCredentialsAsync(invalidExtension!);

        SipCredentialsValidationException actualException =
            await Assert.ThrowsAsync<SipCredentialsValidationException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
