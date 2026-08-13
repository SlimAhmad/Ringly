using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;

namespace Ringly.Trunking.Asterisk.Tests.Unit.Services.Foundations.Trunks;

public partial class SipTrunkFoundationServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("5555550100")]
    [InlineData("+0555550100")]
    [InlineData("+1")]
    [InlineData("not-a-number")]
    public async Task ShouldThrowValidationExceptionOnDialOutIfPhoneNumberIsInvalidAndLogItAsync(
        string? invalidPhoneNumber)
    {
        // given
        string trunkName = CreateRandomTrunkName();
        var invalidDialOutRequestException = new InvalidDialOutRequestException();

        invalidDialOutRequestException.UpsertDataList(
            key: "phoneNumber",
            value: "Value must be a valid E.164 phone number (e.g. +15555550100)");

        var expectedValidationException = new SipTrunkValidationException(invalidDialOutRequestException);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(invalidPhoneNumber!, trunkName);

        SipTrunkValidationException actualException =
            await Assert.ThrowsAsync<SipTrunkValidationException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.sipTrunkBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnDialOutIfTrunkNameIsInvalidAndLogItAsync(
        string? invalidTrunkName)
    {
        // given
        string phoneNumber = CreateRandomDomesticPhoneNumber();
        var invalidDialOutRequestException = new InvalidDialOutRequestException();

        invalidDialOutRequestException.UpsertDataList(
            key: "trunkName",
            value: "Value is required");

        var expectedValidationException = new SipTrunkValidationException(invalidDialOutRequestException);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(phoneNumber, invalidTrunkName!);

        SipTrunkValidationException actualException =
            await Assert.ThrowsAsync<SipTrunkValidationException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.sipTrunkBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnDialOutIfInternationalDialingNotEnabledAndLogItAsync()
    {
        // given
        SipTrunkConfig config = CreateRandomSipTrunkConfig();
        config.InternationalDialingEnabled = false;
        config.AllowedDestinationCountryCodes = ["44"];
        string phoneNumber = CreateRandomInternationalPhoneNumber("44");

        var blockedDestinationException = new BlockedDestinationException(phoneNumber);
        var expectedValidationException = new SipTrunkValidationException(blockedDestinationException);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName))
                .ReturnsAsync(config);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(phoneNumber, config.TrunkName);

        SipTrunkValidationException actualException =
            await Assert.ThrowsAsync<SipTrunkValidationException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnDialOutIfCountryCodeNotExplicitlyAllowedAndLogItAsync()
    {
        // given
        SipTrunkConfig config = CreateRandomSipTrunkConfig();
        config.InternationalDialingEnabled = true;
        config.AllowedDestinationCountryCodes = ["44"];
        string phoneNumber = CreateRandomInternationalPhoneNumber("234");

        var blockedDestinationException = new BlockedDestinationException(phoneNumber);
        var expectedValidationException = new SipTrunkValidationException(blockedDestinationException);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName))
                .ReturnsAsync(config);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(phoneNumber, config.TrunkName);

        SipTrunkValidationException actualException =
            await Assert.ThrowsAsync<SipTrunkValidationException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnDialOutIfOverConcurrencyLimitAndLogItAsync()
    {
        // given
        SipTrunkConfig config = CreateRandomSipTrunkConfig();
        config.MaxConcurrentCallsPerTrunk = 3;
        string phoneNumber = CreateRandomDomesticPhoneNumber();

        var status = new TrunkCallLimitStatus
        {
            TrunkName = config.TrunkName,
            ActiveCallCount = config.MaxConcurrentCallsPerTrunk,
            SpendTodayUsd = 0m,
            IsOverLimit = false
        };

        var trunkSpendLimitExceededException = new TrunkSpendLimitExceededException(config.TrunkName);

        var expectedDependencyValidationException =
            new SipTrunkDependencyValidationException(trunkSpendLimitExceededException);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName))
                .ReturnsAsync(status);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(phoneNumber, config.TrunkName);

        SipTrunkDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipTrunkDependencyValidationException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnDialOutIfOverSpendLimitAndLogItAsync()
    {
        // given
        SipTrunkConfig config = CreateRandomSipTrunkConfig();
        config.MaxDailySpendUsd = 50m;
        string phoneNumber = CreateRandomDomesticPhoneNumber();

        var status = new TrunkCallLimitStatus
        {
            TrunkName = config.TrunkName,
            ActiveCallCount = 0,
            SpendTodayUsd = 50m,
            IsOverLimit = false
        };

        var trunkSpendLimitExceededException = new TrunkSpendLimitExceededException(config.TrunkName);

        var expectedDependencyValidationException =
            new SipTrunkDependencyValidationException(trunkSpendLimitExceededException);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName))
                .ReturnsAsync(status);

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(phoneNumber, config.TrunkName);

        SipTrunkDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipTrunkDependencyValidationException>(dialOutTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
