using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.Trunking.Asterisk.Tests.Unit.Services.Foundations.Trunks;

public partial class SipTrunkFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnDialOutIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        SipTrunkConfig config = CreateRandomSipTrunkConfig();
        string phoneNumber = CreateRandomDomesticPhoneNumber();
        TrunkCallLimitStatus status = CreateRandomWithinLimitsStatus(config.TrunkName);
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidDialOutRequestException = new InvalidDialOutRequestException();

        var expectedException =
            new SipTrunkDependencyValidationException(invalidDialOutRequestException);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName))
                .ReturnsAsync(status);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.DialOutAsync(phoneNumber, config.TrunkName))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(phoneNumber, config.TrunkName);

        SipTrunkDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipTrunkDependencyValidationException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.VerifyFullFlowInvoked(config, phoneNumber);
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnDialOutIfConflictErrorOccursAndLogItAsync()
    {
        // given
        SipTrunkConfig config = CreateRandomSipTrunkConfig();
        string phoneNumber = CreateRandomDomesticPhoneNumber();
        TrunkCallLimitStatus status = CreateRandomWithinLimitsStatus(config.TrunkName);
        var httpResponseConflictException = new HttpResponseConflictException();
        var invalidDialOutRequestException = new InvalidDialOutRequestException();

        var expectedException =
            new SipTrunkDependencyValidationException(invalidDialOutRequestException);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName))
                .ReturnsAsync(status);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.DialOutAsync(phoneNumber, config.TrunkName))
                .ThrowsAsync(httpResponseConflictException);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(phoneNumber, config.TrunkName);

        SipTrunkDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipTrunkDependencyValidationException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.VerifyFullFlowInvoked(config, phoneNumber);
    }

    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnDialOutIfTrunkConfigNotFoundAndLogItAsync()
    {
        // given
        string trunkName = CreateRandomTrunkName();
        string phoneNumber = CreateRandomDomesticPhoneNumber();
        var httpResponseNotFoundException = new HttpResponseNotFoundException();

        var expectedException = new SipTrunkDependencyException(
            "SIP trunk dependency error occurred, contact support.",
            httpResponseNotFoundException);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(trunkName))
                .ThrowsAsync(httpResponseNotFoundException);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(phoneNumber, trunkName);

        SipTrunkDependencyException actualException =
            await Assert.ThrowsAsync<SipTrunkDependencyException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveTrunkConfigAsync(trunkName),
                Times.Once);

        this.sipTrunkBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Exception> CriticalDependencyExceptions() =>
    [
        new HttpResponseUnauthorizedException(),
        new HttpResponseForbiddenException(),
        new HttpRequestException()
    ];

    [Theory]
    [MemberData(nameof(CriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnDialOutIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        SipTrunkConfig config = CreateRandomSipTrunkConfig();
        string phoneNumber = CreateRandomDomesticPhoneNumber();
        TrunkCallLimitStatus status = CreateRandomWithinLimitsStatus(config.TrunkName);

        var expectedException = new SipTrunkDependencyException(
            "SIP trunk dependency error occurred, contact support.",
            dependencyException);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName))
                .ReturnsAsync(status);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.DialOutAsync(phoneNumber, config.TrunkName))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(phoneNumber, config.TrunkName);

        SipTrunkDependencyException actualException =
            await Assert.ThrowsAsync<SipTrunkDependencyException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.VerifyFullFlowInvoked(config, phoneNumber);
    }

    public static TheoryData<Exception> NonCriticalDependencyExceptions() =>
    [
        new HttpResponseInternalServerErrorException(),
        new HttpResponseServiceUnavailableException()
    ];

    [Theory]
    [MemberData(nameof(NonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnDialOutIfErrorOccursAndLogItAsync(Exception dependencyException)
    {
        // given
        SipTrunkConfig config = CreateRandomSipTrunkConfig();
        string phoneNumber = CreateRandomDomesticPhoneNumber();
        TrunkCallLimitStatus status = CreateRandomWithinLimitsStatus(config.TrunkName);

        var expectedException = new SipTrunkDependencyException(
            "SIP trunk dependency error occurred, contact support.",
            dependencyException);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName))
                .ReturnsAsync(status);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.DialOutAsync(phoneNumber, config.TrunkName))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(phoneNumber, config.TrunkName);

        SipTrunkDependencyException actualException =
            await Assert.ThrowsAsync<SipTrunkDependencyException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.VerifyFullFlowInvoked(config, phoneNumber);
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnDialOutIfErrorOccursAndLogItAsync()
    {
        // given
        SipTrunkConfig config = CreateRandomSipTrunkConfig();
        string phoneNumber = CreateRandomDomesticPhoneNumber();
        TrunkCallLimitStatus status = CreateRandomWithinLimitsStatus(config.TrunkName);
        var exception = new Exception();
        var failedSipTrunkServiceException = new FailedSipTrunkServiceException(exception);
        var expectedException = new SipTrunkServiceException(failedSipTrunkServiceException);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName))
                .ReturnsAsync(status);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.DialOutAsync(phoneNumber, config.TrunkName))
                .ThrowsAsync(exception);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(phoneNumber, config.TrunkName);

        SipTrunkServiceException actualException =
            await Assert.ThrowsAsync<SipTrunkServiceException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.VerifyFullFlowInvoked(config, phoneNumber);
    }

    private void VerifyFullFlowInvoked(SipTrunkConfig config, string phoneNumber)
    {
        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.DialOutAsync(phoneNumber, config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
