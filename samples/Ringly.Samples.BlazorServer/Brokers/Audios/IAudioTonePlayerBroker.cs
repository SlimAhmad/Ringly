namespace Ringly.Samples.BlazorServer.Brokers.Audios;

// Support broker (per the-standard-architecture's broker rules — a generic capability, not tied
// to a single entity) for looped WAV tone playback. This sample has no MAUI, so
// Plugin.Maui.Audio (what Ringly.Samples.BlazorHybrid/Ringly.Samples.Maui use for the same
// dial/ring tones) isn't available — NAudio, already a dependency for real mic/speaker capture
// via the linked CustomWindowsAudioEndPoint, plays that role here too.
public interface IAudioTonePlayerBroker
{
    ValueTask PlayLoopedAsync(Stream toneWav);
    ValueTask StopAsync();
}
