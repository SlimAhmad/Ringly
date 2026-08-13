using Ringly.Abstractions.Models;
using FluentAssertions;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;

namespace Ringly.Trunking.Asterisk.Tests.Acceptance.Services.Foundations.Trunks;

public partial class SipTrunkFoundationServiceAcceptanceTests
{
    [Fact]
    public async Task ShouldRejectBlockedInternationalDestinationAsync()
    {
        // given — default config: AllowedDestinationCountryCodes empty, InternationalDialingEnabled false.
        string trunkName = CreateRandomTrunkName();

        var config = new SipTrunkConfig
        {
            TrunkName = trunkName,
            ProviderHost = "203.0.113.5",
            Username = "trunkuser",
            Password = "trunkpass"
        };

        await this.ConfigureTestTrunkAsync(config);
        string internationalPhoneNumber = "+442071838750";

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(internationalPhoneNumber, trunkName);

        SipTrunkValidationException actualException =
            await Assert.ThrowsAsync<SipTrunkValidationException>(dialOutTask.AsTask);

        // then
        actualException.InnerException.Should().BeOfType<BlockedDestinationException>();
    }

    [Fact]
    public async Task ShouldRejectWhenOverConcurrencyLimitAsync()
    {
        // given — real ActiveCallCount from a fresh instance is 0, so Max=0 guarantees "over".
        string trunkName = CreateRandomTrunkName();

        var config = new SipTrunkConfig
        {
            TrunkName = trunkName,
            ProviderHost = "203.0.113.5",
            Username = "trunkuser",
            Password = "trunkpass",
            MaxConcurrentCallsPerTrunk = 0
        };

        await this.ConfigureTestTrunkAsync(config);
        string domesticPhoneNumber = "+15555550100";

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(domesticPhoneNumber, trunkName);

        SipTrunkDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipTrunkDependencyValidationException>(dialOutTask.AsTask);

        // then
        actualException.InnerException.Should().BeOfType<TrunkSpendLimitExceededException>();
    }

    [Fact]
    public async Task ShouldRejectWhenOverSpendLimitAsync()
    {
        // given — SpendTodayUsd is always 0 (no billing API integration, see row #24), so a
        // zero cap guarantees "over" without needing any real spend to have occurred.
        string trunkName = CreateRandomTrunkName();

        var config = new SipTrunkConfig
        {
            TrunkName = trunkName,
            ProviderHost = "203.0.113.5",
            Username = "trunkuser",
            Password = "trunkpass",
            MaxDailySpendUsd = 0m
        };

        await this.ConfigureTestTrunkAsync(config);
        string domesticPhoneNumber = "+15555550100";

        // when
        ValueTask<Channel> dialOutTask =
            this.sipTrunkFoundationService.DialOutAsync(domesticPhoneNumber, trunkName);

        SipTrunkDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipTrunkDependencyValidationException>(dialOutTask.AsTask);

        // then
        actualException.InnerException.Should().BeOfType<TrunkSpendLimitExceededException>();
    }
}
