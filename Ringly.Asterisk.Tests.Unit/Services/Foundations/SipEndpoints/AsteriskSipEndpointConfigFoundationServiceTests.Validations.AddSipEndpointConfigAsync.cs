using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.SipEndpoints;

public partial class AsteriskSipEndpointConfigFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnAddIfConfigIsNullAndLogItAsync()
    {
        // given
        SipEndpointConfig nullConfig = null!;
        var nullSipEndpointConfigException = new NullSipEndpointConfigException();

        var expectedValidationException =
            new SipEndpointConfigValidationException(nullSipEndpointConfigException);

        // when
        ValueTask addTask = this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(nullConfig);

        SipEndpointConfigValidationException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigValidationException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, "somePassword")]
    [InlineData("", "somePassword")]
    [InlineData(" ", "somePassword")]
    [InlineData("someExtension", null)]
    [InlineData("someExtension", "")]
    [InlineData("someExtension", " ")]
    public async Task ShouldThrowValidationExceptionOnAddIfConfigIsInvalidAndLogItAsync(
        string? extension, string? password)
    {
        // given
        var invalidConfig = new SipEndpointConfig
        {
            Extension = extension!,
            Password = password!
        };

        var invalidSipEndpointConfigException = new InvalidSipEndpointConfigException();

        if (string.IsNullOrWhiteSpace(extension))
        {
            invalidSipEndpointConfigException.UpsertDataList(
                key: nameof(SipEndpointConfig.Extension),
                value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            invalidSipEndpointConfigException.UpsertDataList(
                key: nameof(SipEndpointConfig.Password),
                value: "Value is required");
        }

        var expectedValidationException =
            new SipEndpointConfigValidationException(invalidSipEndpointConfigException);

        // when
        ValueTask addTask = this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(invalidConfig);

        SipEndpointConfigValidationException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigValidationException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
