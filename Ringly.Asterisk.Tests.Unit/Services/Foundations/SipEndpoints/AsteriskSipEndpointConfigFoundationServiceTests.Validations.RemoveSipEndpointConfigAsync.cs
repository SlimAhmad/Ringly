using FluentAssertions;
using Moq;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.SipEndpoints;

public partial class AsteriskSipEndpointConfigFoundationServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnRemoveIfExtensionIsInvalidAndLogItAsync(
        string? invalidExtension)
    {
        // given
        var invalidSipEndpointConfigException = new InvalidSipEndpointConfigException();

        invalidSipEndpointConfigException.UpsertDataList(
            key: "extension",
            value: "Value is required");

        var expectedValidationException =
            new SipEndpointConfigValidationException(invalidSipEndpointConfigException);

        // when
        ValueTask removeTask =
            this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(invalidExtension!);

        SipEndpointConfigValidationException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigValidationException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
