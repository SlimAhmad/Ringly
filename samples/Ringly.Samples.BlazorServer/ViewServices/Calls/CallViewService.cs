using Ringly.Client.Abstractions;
using Ringly.Client.Abstractions.Models;
using Ringly.Samples.BlazorServer.Brokers.Audios;
using Ringly.Samples.BlazorServer.Models.Calls;
using Ringly.Samples.BlazorServer.Video;
using Ringly.Samples.Maui;

namespace Ringly.Samples.BlazorServer.ViewServices.Calls;

public sealed class CallViewService : ICallViewService
{
    private readonly ICallClient callClient;
    private readonly IAudioTonePlayerBroker audioTonePlayerBroker;
    private readonly IVideoFramePreviewSource videoFramePreviewSource;

    private readonly List<string> eventLog = [];
    private IDisposable? eventSubscription;
    private System.Threading.Timer? callTimer;
    private CallHandle? currentCallHandle;
    private CallHandle? incomingCallHandle;
    private DateTimeOffset callAnsweredAt;

    public event Action? StateChanged;

    public CallScreenState State { get; private set; } = CallScreenState.Setup;

    public string Extension { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string TargetExtension { get; set; } = string.Empty;

    public string RegistrationStatus { get; private set; } = string.Empty;
    public string RegistrationStatusColorClass { get; private set; } = string.Empty;

    public string CallStateLabel { get; private set; } = string.Empty;
    public string CallTargetExtension { get; private set; } = string.Empty;
    public string CallElapsedText { get; private set; } = "00:00";
    public string? CallFailedMessage { get; private set; }

    public bool IsMuted { get; private set; }
    public bool IsVideoMuted { get; private set; }

    // Whether the CURRENT call actually offered video — an audio call never starts the camera
    // session at all (no video "m=" line to negotiate a format against).
    public bool CurrentCallIncludesVideo { get; private set; }

    public string? RemoteVideoDataUri { get; private set; }

    public IReadOnlyList<string> EventLog => this.eventLog;

    public CallViewService(
        ICallClient callClient,
        IAudioTonePlayerBroker audioTonePlayerBroker,
        IVideoFramePreviewSource videoFramePreviewSource)
    {
        this.callClient = callClient;
        this.audioTonePlayerBroker = audioTonePlayerBroker;
        this.videoFramePreviewSource = videoFramePreviewSource;
    }

    public ValueTask InitializeAsync()
    {
        this.eventSubscription = this.callClient.StreamEvents().Subscribe(this.OnCallEvent);
        this.videoFramePreviewSource.RemoteFrameDataUriReady += this.OnRemoteFrameDataUriReady;
        return ValueTask.CompletedTask;
    }

    public async ValueTask RegisterAsync()
    {
        this.RegistrationStatus = "Registering...";
        this.RegistrationStatusColorClass = "text-amber-400";
        this.OnStateChanged();

        try
        {
            await this.callClient.RegisterAsync(new SipCredentials
            {
                ClientId = Guid.NewGuid(),
                Extension = this.Extension,
                Password = this.Password
            });

            this.RegistrationStatus = $"Registered as {this.Extension}";
            this.RegistrationStatusColorClass = "text-emerald-400";
        }
        catch (Exception exception)
        {
            this.RegistrationStatus = $"Registration failed: {exception.Message}";
            this.RegistrationStatusColorClass = "text-red-400";
        }

        this.OnStateChanged();
    }

    public ValueTask PlaceAudioCallAsync() => this.PlaceCallAsync(includeVideo: false);

    public ValueTask PlaceVideoCallAsync() => this.PlaceCallAsync(includeVideo: true);

    private async ValueTask PlaceCallAsync(bool includeVideo)
    {
        if (string.IsNullOrWhiteSpace(this.TargetExtension))
        {
            return;
        }

        this.CurrentCallIncludesVideo = includeVideo;
        await this.ShowDialingAsync(this.TargetExtension);

        try
        {
            this.currentCallHandle = await this.callClient.PlaceCallAsync(this.TargetExtension, includeVideo);
        }
        catch (Exception exception)
        {
            this.LogEvent($"Call failed: {exception.Message}");
            await this.ShowCallFailedAsync();
        }

        this.OnStateChanged();
    }

    public async ValueTask HangupAsync()
    {
        CallHandle? handle = this.currentCallHandle ?? this.incomingCallHandle;

        if (handle is not null)
        {
            try
            {
                await this.callClient.HangupAsync(handle);
            }
            catch (Exception exception)
            {
                this.LogEvent($"Hangup failed: {exception.Message}");
            }
        }

        this.currentCallHandle = null;
        this.incomingCallHandle = null;
        await this.ShowSetupAsync();
        this.OnStateChanged();
    }

    public async ValueTask AnswerAsync()
    {
        if (this.incomingCallHandle is null)
        {
            return;
        }

        try
        {
            await this.callClient.AnswerCallAsync(this.incomingCallHandle);
            this.currentCallHandle = this.incomingCallHandle;
            await this.ShowActiveAsync();
        }
        catch (Exception exception)
        {
            this.LogEvent($"Answer failed: {exception.Message}");
            await this.ShowCallFailedAsync();
        }

        this.OnStateChanged();
    }

    public async ValueTask DeclineAsync()
    {
        if (this.incomingCallHandle is not null)
        {
            try
            {
                await this.callClient.HangupAsync(this.incomingCallHandle);
            }
            catch (Exception exception)
            {
                this.LogEvent($"Decline failed: {exception.Message}");
            }

            this.incomingCallHandle = null;
        }

        await this.ShowSetupAsync();
        this.OnStateChanged();
    }

    public async ValueTask ToggleMuteAsync()
    {
        try
        {
            if (this.IsMuted)
            {
                await this.callClient.UnmuteAsync();
            }
            else
            {
                await this.callClient.MuteAsync();
            }

            this.IsMuted = !this.IsMuted;
        }
        catch (Exception exception)
        {
            this.LogEvent($"Mute failed: {exception.Message}");
        }

        this.OnStateChanged();
    }

    public async ValueTask ToggleVideoMuteAsync()
    {
        try
        {
            if (this.IsVideoMuted)
            {
                await this.callClient.UnmuteVideoAsync();
            }
            else
            {
                await this.callClient.MuteVideoAsync();
            }

            this.IsVideoMuted = !this.IsVideoMuted;
        }
        catch (Exception exception)
        {
            this.LogEvent($"Video toggle failed: {exception.Message}");
        }

        this.OnStateChanged();
    }

    private void OnRemoteFrameDataUriReady(string dataUri)
    {
        this.RemoteVideoDataUri = dataUri;
        this.OnStateChanged();
    }

    private void OnCallEvent(CallClientEvent callEvent)
    {
        this.LogEvent($"{callEvent.EventType}");

        switch (callEvent.EventType)
        {
            case "IncomingCall":
                this.incomingCallHandle = callEvent.Handle;
                this.CurrentCallIncludesVideo = callEvent.IncludesVideo;
                _ = this.ShowIncomingAsync(callEvent.RemoteExtension);
                break;

            case "CallAnswered":
                _ = this.ShowActiveAsync();
                break;

            case "CallHungup":
                this.currentCallHandle = null;
                this.incomingCallHandle = null;
                _ = this.ShowSetupAsync();
                break;

            case "CallFailed":
                this.currentCallHandle = null;
                this.incomingCallHandle = null;
                _ = this.ShowCallFailedAsync();
                break;
        }

        this.OnStateChanged();
    }

    private async ValueTask ShowSetupAsync()
    {
        this.StopCallTimer();
        await this.audioTonePlayerBroker.StopAsync();
        this.State = CallScreenState.Setup;
        this.IsMuted = false;
        this.IsVideoMuted = false;
        this.CurrentCallIncludesVideo = false;
        this.CallFailedMessage = null;
        this.RemoteVideoDataUri = null;
    }

    private async ValueTask ShowDialingAsync(string extensionValue)
    {
        this.State = CallScreenState.Dialing;
        this.CallFailedMessage = null;
        this.CallStateLabel = "Calling";
        this.CallTargetExtension = extensionValue;
        await this.audioTonePlayerBroker.PlayLoopedAsync(ToneGenerator.CreateRingbackTone());
    }

    private async ValueTask ShowIncomingAsync(string callerExtension)
    {
        this.State = CallScreenState.Incoming;
        this.CallFailedMessage = null;
        this.CallStateLabel = "Incoming call";
        this.CallTargetExtension = string.IsNullOrEmpty(callerExtension) ? "Unknown" : callerExtension;
        await this.audioTonePlayerBroker.PlayLoopedAsync(ToneGenerator.CreateRingTone());
        this.OnStateChanged();
    }

    private async ValueTask ShowActiveAsync()
    {
        await this.audioTonePlayerBroker.StopAsync();
        this.State = CallScreenState.Active;
        this.CallFailedMessage = null;
        this.CallStateLabel = "In call with";
        this.StartCallTimer();
        this.OnStateChanged();
    }

    private async ValueTask ShowCallFailedAsync()
    {
        this.StopCallTimer();
        await this.audioTonePlayerBroker.StopAsync();
        this.State = CallScreenState.Setup;
        this.CallFailedMessage = "Call ended unexpectedly — see event log below.";
        this.OnStateChanged();
    }

    private void StartCallTimer()
    {
        this.callAnsweredAt = DateTimeOffset.Now;
        this.CallElapsedText = "00:00";

        this.callTimer = new System.Threading.Timer(_ =>
        {
            TimeSpan elapsed = DateTimeOffset.Now - this.callAnsweredAt;
            this.CallElapsedText = elapsed.ToString(elapsed.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
            this.OnStateChanged();
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void StopCallTimer()
    {
        this.callTimer?.Dispose();
        this.callTimer = null;
    }

    private void LogEvent(string message) =>
        this.eventLog.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} {message}");

    private void OnStateChanged() => this.StateChanged?.Invoke();

    public void Dispose()
    {
        this.eventSubscription?.Dispose();
        this.videoFramePreviewSource.RemoteFrameDataUriReady -= this.OnRemoteFrameDataUriReady;
        this.StopCallTimer();
    }
}
