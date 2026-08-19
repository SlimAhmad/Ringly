using Ringly.Samples.BlazorServer.Models.Calls;

namespace Ringly.Samples.BlazorServer.ViewServices.Calls;

// The single dependency CallScreen.razor (the Core Component) integrates with — per
// the-standard-architecture's UI rules, a Core Component "behaves like a service/controller
// hybrid" through exactly one view service, and never talks to ICallClient/IAudioTonePlayerBroker/
// IVideoFramePreviewSource directly itself.
public interface ICallViewService : IDisposable
{
    // Raised whenever state changes that CallScreen didn't itself just trigger via a user
    // action (call-lifecycle events, the once-a-second call timer) — CallScreen subscribes and
    // re-renders. User-driven changes (typing into a bound input) don't need this: Blazor already
    // re-renders the component that owns an @bind'd property after its own event handler runs.
    event Action? StateChanged;

    CallScreenState State { get; }

    string Extension { get; set; }
    string Password { get; set; }
    string TargetExtension { get; set; }

    string RegistrationStatus { get; }
    string RegistrationStatusColorClass { get; }

    string CallStateLabel { get; }
    string CallTargetExtension { get; }
    string CallElapsedText { get; }
    string? CallFailedMessage { get; }

    bool IsMuted { get; }
    bool IsVideoMuted { get; }
    bool CurrentCallIncludesVideo { get; }
    string? RemoteVideoDataUri { get; }

    IReadOnlyList<string> EventLog { get; }

    ValueTask InitializeAsync();
    ValueTask RegisterAsync();
    ValueTask PlaceAudioCallAsync();
    ValueTask PlaceVideoCallAsync();
    ValueTask HangupAsync();
    ValueTask AnswerAsync();
    ValueTask DeclineAsync();
    ValueTask ToggleMuteAsync();
    ValueTask ToggleVideoMuteAsync();
}
