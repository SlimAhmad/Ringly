using Moq;
using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Tests.Unit.Services.Monitoring.SpendAlerts;

public partial class TrunkSpendAlertServiceTests
{
    [Fact]
    public async Task ShouldNotNotifyWhenAllTrunksWithinLimitsAsync()
    {
        // given
        string trunkName = GetRandomString();
        SipTrunkConfig config = CreateRandomSipTrunkConfig(trunkName);
        TrunkCallLimitStatus status = CreateWithinLimitsStatus(trunkName);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.ListConfiguredTrunkNamesAsync())
                .ReturnsAsync([trunkName]);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(trunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(trunkName))
                .ReturnsAsync(status);

        // when
        await this.trunkSpendAlertService.PollAsync();

        // then
        this.sipTrunkBrokerMock.Verify(broker =>
            broker.ListConfiguredTrunkNamesAsync(),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveTrunkConfigAsync(trunkName),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveSpendStatusAsync(trunkName),
                Times.Once);

        this.sipTrunkBrokerMock.VerifyNoOtherCalls();
        this.alertNotifierMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldNotifyWhenOverConcurrencyLimitAsync()
    {
        // given
        string trunkName = GetRandomString();
        SipTrunkConfig config = CreateRandomSipTrunkConfig(trunkName);

        var status = new TrunkCallLimitStatus
        {
            TrunkName = trunkName,
            ActiveCallCount = config.MaxConcurrentCallsPerTrunk,
            SpendTodayUsd = 0m,
            IsOverLimit = false
        };

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.ListConfiguredTrunkNamesAsync())
                .ReturnsAsync([trunkName]);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(trunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(trunkName))
                .ReturnsAsync(status);

        // when
        await this.trunkSpendAlertService.PollAsync();

        // then
        this.alertNotifierMock.Verify(notifier =>
            notifier.NotifyAsync(trunkName, status),
                Times.Once);

        this.alertNotifierMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldNotifyWhenOverSpendLimitAsync()
    {
        // given
        string trunkName = GetRandomString();
        SipTrunkConfig config = CreateRandomSipTrunkConfig(trunkName);

        var status = new TrunkCallLimitStatus
        {
            TrunkName = trunkName,
            ActiveCallCount = 0,
            SpendTodayUsd = config.MaxDailySpendUsd!.Value,
            IsOverLimit = false
        };

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.ListConfiguredTrunkNamesAsync())
                .ReturnsAsync([trunkName]);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(trunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(trunkName))
                .ReturnsAsync(status);

        // when
        await this.trunkSpendAlertService.PollAsync();

        // then
        this.alertNotifierMock.Verify(notifier =>
            notifier.NotifyAsync(trunkName, status),
                Times.Once);

        this.alertNotifierMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldNotAlertWhenNoDailySpendCapConfiguredAsync()
    {
        // given
        string trunkName = GetRandomString();
        SipTrunkConfig config = CreateRandomSipTrunkConfig(trunkName);
        config.MaxDailySpendUsd = null;

        var status = new TrunkCallLimitStatus
        {
            TrunkName = trunkName,
            ActiveCallCount = 0,
            SpendTodayUsd = 999999m,
            IsOverLimit = false
        };

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.ListConfiguredTrunkNamesAsync())
                .ReturnsAsync([trunkName]);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(trunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(trunkName))
                .ReturnsAsync(status);

        // when
        await this.trunkSpendAlertService.PollAsync();

        // then
        this.alertNotifierMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldLogAndContinuePollingOtherTrunksWhenOneTrunkErrorsAsync()
    {
        // given
        string failingTrunkName = GetRandomString();
        string healthyTrunkName = GetRandomString();
        SipTrunkConfig healthyConfig = CreateRandomSipTrunkConfig(healthyTrunkName);
        TrunkCallLimitStatus healthyStatus = CreateWithinLimitsStatus(healthyTrunkName);
        var exception = new Exception();

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.ListConfiguredTrunkNamesAsync())
                .ReturnsAsync([failingTrunkName, healthyTrunkName]);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(failingTrunkName))
                .ThrowsAsync(exception);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(healthyTrunkName))
                .ReturnsAsync(healthyConfig);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(healthyTrunkName))
                .ReturnsAsync(healthyStatus);

        // when
        await this.trunkSpendAlertService.PollAsync();

        // then
        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(exception),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveTrunkConfigAsync(healthyTrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveSpendStatusAsync(healthyTrunkName),
                Times.Once);

        this.alertNotifierMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
