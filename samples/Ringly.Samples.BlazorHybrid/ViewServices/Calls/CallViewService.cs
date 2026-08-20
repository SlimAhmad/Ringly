using Plugin.Maui.Audio;
using Ringly.Client.Abstractions;
using Ringly.Client.Abstractions.Models;
using Ringly.Samples.BlazorHybrid.Models.Calls;
using Ringly.Samples.Maui;

namespace Ringly.Samples.BlazorHybrid.ViewServices.Calls;

public sealed class CallViewService : ICallViewService
{
    // Same 100ms-per-frame (10fps) throttle rationale as Ringly.Samples.Maui's CallPage.xaml.cs —
    // reassigning an <img> src faster than the browser/WebView can decode the previous one queues
    // up redundant work for no visible benefit; a generous cap relative to the video pipeline's
    // own 15fps encode throttle on both platforms.
    private static readonly TimeSpan MinImageUpdateInterval = TimeSpan.FromMilliseconds(100);

    private readonly ICallClient callClient;
    private readonly IAudioManager audioManager;
    private readonly IVideoFramePreviewSource videoPreviewSource;

    private readonly List<string> eventLog = [];
    private IDisposable? eventSubscription;
    private System.Threading.Timer? callTimer;
    private IAudioPlayer? tonePlayer;
    private CallHandle? currentCallHandle;
    private CallHandle? incomingCallHandle;
    private DateTimeOffset callAnsweredAt;
    private DateTime lastRemoteImageUpdateAt = DateTime.MinValue;
    private DateTime lastLocalImageUpdateAt = DateTime.MinValue;

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

    public string? RemoteVideoDataUri { get; private set; }
    public string? LocalVideoDataUri { get; private set; }

    public bool IsMuted { get; private set; }
    public bool IsVideoMuted { get; private set; }

    // Whether the CURRENT call actually offered video — an audio call never starts the camera
    // session at all (no video "m=" line to negotiate a format against).
    public bool CurrentCallIncludesVideo { get; private set; }

    public IReadOnlyList<string> EventLog => this.eventLog;

    public CallViewService(
        ICallClient callClient,
        IAudioManager audioManager,
        IVideoFramePreviewSource videoPreviewSource)
    {
        this.callClient = callClient;
        this.audioManager = audioManager;
        this.videoPreviewSource = videoPreviewSource;
    }

    public async ValueTask InitializeAsync()
    {
        this.eventSubscription = this.callClient.StreamEvents().Subscribe(this.OnCallEvent);
        this.videoPreviewSource.DecodedFrameReady += this.OnDecodedFrameReady;
        this.videoPreviewSource.LocalFrameReady += this.OnLocalFrameReady;

#if ANDROID
        // AndroidAudioEndPoint's AudioRecord and the camera capture pipeline both silently fail to
        // initialize without these — Android 6+ requires an explicit runtime grant even though
        // both are declared in AndroidManifest.xml. Requested once up front so a denial surfaces
        // immediately instead of as a confusing silent "no audio"/"no video" later.
        PermissionStatus microphoneStatus = await Permissions.RequestAsync<Permissions.Microphone>();

        if (microphoneStatus != PermissionStatus.Granted)
        {
            this.LogEvent("Microphone permission denied — calls will have no audio.");
        }

        PermissionStatus cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();

        if (cameraStatus != PermissionStatus.Granted)
        {
            this.LogEvent("Camera permission denied — calls will have no outgoing video.");
        }
#else
        await Task.CompletedTask;
#endif
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
        this.ShowDialing(this.TargetExtension);

        try
        {
            this.currentCallHandle = await this.callClient.PlaceCallAsync(this.TargetExtension, includeVideo);
        }
        catch (Exception exception)
        {
            this.LogEvent($"Call failed: {exception.Message}");
            this.ShowCallFailed();
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
        this.ShowSetup();
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
            this.ShowActive();
        }
        catch (Exception exception)
        {
            this.LogEvent($"Answer failed: {exception.Message}");
            this.ShowCallFailed();
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

        this.ShowSetup();
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

    public async ValueTask SwitchCameraAsync()
    {
        try
        {
            await this.videoPreviewSource.SwitchCameraAsync();
        }
        catch (Exception exception)
        {
            this.LogEvent($"Camera switch failed: {exception.Message}");
        }
    }

    private void OnCallEvent(CallClientEvent callEvent)
    {
        this.LogEvent($"{callEvent.EventType}");

        switch (callEvent.EventType)
        {
            case "IncomingCall":
                this.incomingCallHandle = callEvent.Handle;
                this.CurrentCallIncludesVideo = callEvent.IncludesVideo;
                this.ShowIncoming(callEvent.RemoteExtension);
                break;

            case "CallAnswered":
                this.ShowActive();
                break;

            case "CallHungup":
                this.currentCallHandle = null;
                this.incomingCallHandle = null;
                this.ShowSetup();
                break;

            case "CallFailed":
                this.currentCallHandle = null;
                this.incomingCallHandle = null;
                this.ShowCallFailed();
                break;
        }

        this.OnStateChanged();
    }

    // Runs off the capture/decode thread — the Core Component marshals to the UI thread itself
    // (via InvokeAsync) when it handles StateChanged, so this only needs to update state and
    // raise the event.
    private void OnDecodedFrameReady(int width, int height, byte[] bgrSample)
    {
        if (DateTime.UtcNow - this.lastRemoteImageUpdateAt < MinImageUpdateInterval)
        {
            return;
        }

        this.lastRemoteImageUpdateAt = DateTime.UtcNow;
        this.RemoteVideoDataUri = BuildBitmapDataUri(width, height, bgrSample);
        this.OnStateChanged();
    }

    private void OnLocalFrameReady(int width, int height, byte[] bgrSample)
    {
        if (DateTime.UtcNow - this.lastLocalImageUpdateAt < MinImageUpdateInterval)
        {
            return;
        }

        this.lastLocalImageUpdateAt = DateTime.UtcNow;
        this.LocalVideoDataUri = BuildBitmapDataUri(width, height, bgrSample);
        this.OnStateChanged();
    }

    // Wraps a raw BGR24 buffer (VP8Codec.DecodeVideo's output format) in a minimal 24bpp BMP
    // container, base64-encoded as a data: URI an <img> can render directly — the Blazor
    // equivalent of CallPage.xaml.cs's BuildBitmap, which does the same thing for a MAUI
    // ImageSource. Stores rows bottom-up (BMP's canonical, universally-supported layout).
    private static string BuildBitmapDataUri(int width, int height, byte[] bgr)
    {
        int rowSize = ((width * 3) + 3) / 4 * 4;
        int pixelDataSize = rowSize * height;
        int fileSize = 54 + pixelDataSize;

        var bitmap = new byte[fileSize];
        bitmap[0] = (byte)'B';
        bitmap[1] = (byte)'M';
        BitConverter.GetBytes(fileSize).CopyTo(bitmap, 2);
        BitConverter.GetBytes(54).CopyTo(bitmap, 10);
        BitConverter.GetBytes(40).CopyTo(bitmap, 14);
        BitConverter.GetBytes(width).CopyTo(bitmap, 18);
        BitConverter.GetBytes(height).CopyTo(bitmap, 22);
        BitConverter.GetBytes((short)1).CopyTo(bitmap, 26);
        BitConverter.GetBytes((short)24).CopyTo(bitmap, 28);
        BitConverter.GetBytes(pixelDataSize).CopyTo(bitmap, 34);

        for (int row = 0; row < height; row++)
        {
            int sourceRowStart = row * width * 3;
            int destinationRowStart = 54 + ((height - 1 - row) * rowSize);
            Array.Copy(bgr, sourceRowStart, bitmap, destinationRowStart, width * 3);
        }

        return $"data:image/bmp;base64,{Convert.ToBase64String(bitmap)}";
    }

    private void ShowSetup()
    {
        this.StopCallTimer();
        this.StopTone();
        this.State = CallScreenState.Setup;
        this.IsMuted = false;
        this.IsVideoMuted = false;
        this.CurrentCallIncludesVideo = false;
        this.CallFailedMessage = null;
        this.RemoteVideoDataUri = null;
        this.LocalVideoDataUri = null;
    }

    private void ShowDialing(string extensionValue)
    {
        this.State = CallScreenState.Dialing;
        this.CallFailedMessage = null;
        this.CallStateLabel = "Calling";
        this.CallTargetExtension = extensionValue;
        this.PlayTone(ToneGenerator.CreateRingbackTone());
    }

    private void ShowIncoming(string callerExtension)
    {
        this.State = CallScreenState.Incoming;
        this.CallFailedMessage = null;
        this.CallStateLabel = "Incoming call";
        this.CallTargetExtension = string.IsNullOrEmpty(callerExtension) ? "Unknown" : callerExtension;
        this.PlayTone(ToneGenerator.CreateRingTone());
    }

    private void ShowActive()
    {
        this.StopTone();
        this.State = CallScreenState.Active;
        this.CallFailedMessage = null;
        this.CallStateLabel = "In call with";
        this.StartCallTimer();
    }

    private void ShowCallFailed()
    {
        this.StopCallTimer();
        this.StopTone();
        this.State = CallScreenState.Setup;
        this.CallFailedMessage = "Call ended unexpectedly — see event log below.";
    }

    private void PlayTone(Stream toneWav)
    {
        this.StopTone();

        try
        {
            this.tonePlayer = this.audioManager.CreatePlayer(toneWav);
            this.tonePlayer.Loop = true;
            this.tonePlayer.Play();
        }
        catch (Exception exception)
        {
            this.LogEvent($"Tone playback failed: {exception.Message}");
        }
    }

    private void StopTone()
    {
        this.tonePlayer?.Stop();
        this.tonePlayer?.Dispose();
        this.tonePlayer = null;
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
        this.videoPreviewSource.DecodedFrameReady -= this.OnDecodedFrameReady;
        this.videoPreviewSource.LocalFrameReady -= this.OnLocalFrameReady;
        this.StopCallTimer();
        this.StopTone();
    }
}
