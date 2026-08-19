namespace Ringly.Samples.BlazorHybrid.Brokers.Apis;

// Brokers own their configurations (the-standard-architecture) — configured in MauiProgram.cs
// alongside SipSorceryCallOptions, using the same host-resolution logic (this project has no
// appsettings.json; network config lives as constants in MauiProgram.cs, matching its existing
// SIP/ICE configuration style).
public class SupportApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
}
