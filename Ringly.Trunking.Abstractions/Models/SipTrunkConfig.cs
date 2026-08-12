namespace Ringly.Trunking.Abstractions.Models;

public class SipTrunkConfig
{
    public string TrunkName { get; set; } = string.Empty;
    public string ProviderHost { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // e.g. ["234"] — empty/null means domestic-only default
    public List<string>? AllowedDestinationCountryCodes { get; set; }

    // must be explicitly opted in
    public bool InternationalDialingEnabled { get; set; } = false;

    // soft cap enforced app-side; hard cap must also be set at the provider
    public decimal? MaxDailySpendUsd { get; set; }

    // throttle — limits blast radius of compromised credentials
    public int MaxConcurrentCallsPerTrunk { get; set; } = 5;
}
