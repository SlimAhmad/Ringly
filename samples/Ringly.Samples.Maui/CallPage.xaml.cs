using System.Collections.ObjectModel;
using Plugin.Maui.Audio;
using Ringly.Client.Abstractions;
using Ringly.Client.Abstractions.Models;

namespace Ringly.Samples.Maui;

public partial class CallPage : ContentPage
{
    private readonly ICallClient callClient;
    private readonly IAudioManager audioManager;
    private readonly ObservableCollection<string> eventLog = [];
    private IDisposable? eventSubscription;
    private IDispatcherTimer? callTimer;
    private IAudioPlayer? tonePlayer;
    private CallHandle? currentCallHandle;
    private CallHandle? incomingCallHandle;
    private DateTimeOffset callAnsweredAt;
    private bool isMuted;
    private bool isSpeakerOn;
    private bool isVideoMuted;

    // Throttles how often RemoteVideoImage/LocalVideoImage.Source gets reassigned — confirmed live
    // that reassigning ImageSource.FromStream faster than MAUI's underlying image decode can finish
    // makes it cancel the in-flight decode, logged as a noisy
    // "Microsoft.Maui.StreamImageSourceService: Unable to load image stream" / TaskCanceledException
    // warning (19 in one 65s call). 100ms (10fps) is a generous cap relative to the video pipeline's
    // own throttles (15fps encode, 6fps local preview) — this just prevents back-to-back decoded
    // frames from queuing overlapping reassignments faster than the UI can actually render them.
    private static readonly TimeSpan MinImageUpdateInterval = TimeSpan.FromMilliseconds(100);
    private DateTime lastRemoteImageUpdateAt = DateTime.MinValue;
    private DateTime lastLocalImageUpdateAt = DateTime.MinValue;

    // Whether the CURRENT call actually offered video — an Audio Call (see
    // OnAudioCallClicked/PlaceCallAsync) never calls IVideoSource.StartVideo() at all (no video
    // "m=" line to negotiate a format against), so the camera session genuinely never starts even
    // though a video endpoint is registered. Set from PlaceCallAsync's own includeVideo argument
    // for outgoing calls, and from CallClientEvent.IncludesVideo (parsed from the real SDP offer
    // by SipSorceryCallClient.HandleIncomingCall) for incoming ones.
    private bool currentCallIncludesVideo;

#if WINDOWS
    private readonly Platforms.Windows.CustomWindowsVideoEndPoint? windowsVideoEndPoint;

    public CallPage(ICallClient callClient, IAudioManager audioManager, Platforms.Windows.CustomWindowsVideoEndPoint windowsVideoEndPoint)
    {
        InitializeComponent();
        this.callClient = callClient;
        this.audioManager = audioManager;
        this.windowsVideoEndPoint = windowsVideoEndPoint;
        this.EventLogView.ItemsSource = this.eventLog;
    }
#elif ANDROID
    // Typed to the shared interface, not either concrete implementation — see
    // IAndroidVideoCaptureEndPoint.cs for why (A/B testing Shiny.Maui.Controls.Camera's capture
    // pipeline against a hand-rolled CameraX one, per #143). MauiProgram.cs's DI registration
    // alone decides which concrete type this resolves to.
    private readonly Platforms.Android.IAndroidVideoCaptureEndPoint? androidVideoEndPoint;

    public CallPage(ICallClient callClient, IAudioManager audioManager, Platforms.Android.IAndroidVideoCaptureEndPoint androidVideoEndPoint)
    {
        InitializeComponent();
        this.callClient = callClient;
        this.audioManager = audioManager;
        this.androidVideoEndPoint = androidVideoEndPoint;
        this.EventLogView.ItemsSource = this.eventLog;
    }
#else
    public CallPage(ICallClient callClient, IAudioManager audioManager)
    {
        InitializeComponent();
        this.callClient = callClient;
        this.audioManager = audioManager;
        this.EventLogView.ItemsSource = this.eventLog;
    }
#endif

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        this.eventSubscription = this.callClient.StreamEvents().Subscribe(callEvent =>
            this.Dispatcher.Dispatch(() => this.HandleCallEvent(callEvent)));

#if WINDOWS
        if (this.windowsVideoEndPoint is not null)
        {
            this.windowsVideoEndPoint.AttachCameraView(this.LocalCameraView);
            this.windowsVideoEndPoint.DecodedFrameReady += this.OnDecodedFrameReady;
        }
#elif ANDROID
        if (this.androidVideoEndPoint is not null)
        {
            this.androidVideoEndPoint.AttachCameraView(this.LocalCameraView);
            this.androidVideoEndPoint.DecodedFrameReady += this.OnDecodedFrameReady;
            this.androidVideoEndPoint.LocalFrameReady += this.OnLocalFrameReady;
        }

        this.SwitchCameraButtonContainer.IsVisible = this.androidVideoEndPoint is not null;

        // AndroidAudioEndPoint's AudioRecord silently fails to initialize without this — Android
        // 6+ requires an explicit runtime grant even though RECORD_AUDIO is declared in the
        // manifest. Requested once up front here rather than deferred until the first call, so a
        // denial surfaces immediately instead of as a confusing silent "no audio" later.
        PermissionStatus microphoneStatus = await Permissions.RequestAsync<Permissions.Microphone>();

        if (microphoneStatus != PermissionStatus.Granted)
        {
            this.eventLog.Insert(0,
                $"{DateTimeOffset.Now:HH:mm:ss} Microphone permission denied — calls will have no audio.");
        }

        // Camera capture needs the same kind of explicit runtime grant as the microphone above —
        // requested once up front for the same reason (a denial should surface immediately, not
        // as a confusing silent "no video" once a call actually starts).
        PermissionStatus cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();

        if (cameraStatus != PermissionStatus.Granted)
        {
            this.eventLog.Insert(0,
                $"{DateTimeOffset.Now:HH:mm:ss} Camera permission denied — calls will have no outgoing video.");
        }
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        this.eventSubscription?.Dispose();
        this.StopCallTimer();
        this.StopTone();

#if WINDOWS
        if (this.windowsVideoEndPoint is not null)
        {
            this.windowsVideoEndPoint.DecodedFrameReady -= this.OnDecodedFrameReady;
            this.windowsVideoEndPoint.DetachCameraView();
        }
#elif ANDROID
        if (this.androidVideoEndPoint is not null)
        {
            this.androidVideoEndPoint.DecodedFrameReady -= this.OnDecodedFrameReady;
            this.androidVideoEndPoint.LocalFrameReady -= this.OnLocalFrameReady;
            this.androidVideoEndPoint.DetachCameraView();
        }
#endif
    }

#if WINDOWS || ANDROID
    // Runs off the capture/decode thread — marshal to the UI thread before touching
    // RemoteVideoImage, same as HandleCallEvent's Dispatcher.Dispatch above.
    private void OnDecodedFrameReady(int width, int height, byte[] bgrSample)
    {
        if (DateTime.UtcNow - this.lastRemoteImageUpdateAt < MinImageUpdateInterval)
        {
            return;
        }

        this.lastRemoteImageUpdateAt = DateTime.UtcNow;

        byte[] bitmap = BuildBitmap(width, height, bgrSample);

        this.Dispatcher.Dispatch(() =>
        {
            this.RemoteVideoImage.Source = ImageSource.FromStream(() => new MemoryStream(bitmap));
            this.RemoteVideoImage.IsVisible = true;
        });
    }

    // Only ever raised by AndroidCameraXVideoEndPoint (see IAndroidVideoCaptureEndPoint's own
    // comment) — Windows and the Shiny-based Android path both render their self-preview through
    // LocalCameraView's own native surface instead, so LocalVideoImage stays hidden there. Since
    // this only fires from that one subscription, being called at all means Android + that
    // implementation + currentCallIncludesVideo were all already true when it was wired up.
    private void OnLocalFrameReady(int width, int height, byte[] bgrSample)
    {
        if (DateTime.UtcNow - this.lastLocalImageUpdateAt < MinImageUpdateInterval)
        {
            return;
        }

        this.lastLocalImageUpdateAt = DateTime.UtcNow;

        byte[] bitmap = BuildBitmap(width, height, bgrSample);

        this.Dispatcher.Dispatch(() =>
        {
            this.LocalVideoImage.Source = ImageSource.FromStream(() => new MemoryStream(bitmap));
            this.LocalVideoImage.IsVisible = this.currentCallIncludesVideo;
        });
    }

    // Wraps a raw BGR24 buffer (VP8Codec.DecodeVideo's output format — confirmed via the "Bgr"
    // pixel format both CustomWindowsVideoEndPoint and AndroidVideoEndPoint request in
    // GotVideoFrame) in a minimal 24bpp BMP container so ImageSource.FromStream can decode it via
    // each platform's own built-in BMP support, without pulling in an imaging library just to
    // preview raw decoded frames. Stores rows bottom-up (BMP's canonical, universally-supported
    // layout — a negative-height top-down header is a Windows-only convenience some decoders,
    // including Android's, don't reliably honor), so rows are written in reverse order rather
    // than copied straight across.
    private static byte[] BuildBitmap(int width, int height, byte[] bgr)
    {
        int rowSize = ((width * 3) + 3) / 4 * 4; // BMP rows are padded to a multiple of 4 bytes
        int pixelDataSize = rowSize * height;
        int fileSize = 54 + pixelDataSize;

        var bitmap = new byte[fileSize];
        bitmap[0] = (byte)'B';
        bitmap[1] = (byte)'M';
        BitConverter.GetBytes(fileSize).CopyTo(bitmap, 2);
        BitConverter.GetBytes(54).CopyTo(bitmap, 10); // pixel data offset
        BitConverter.GetBytes(40).CopyTo(bitmap, 14); // DIB header size (BITMAPINFOHEADER)
        BitConverter.GetBytes(width).CopyTo(bitmap, 18);
        BitConverter.GetBytes(height).CopyTo(bitmap, 22); // positive = bottom-up rows
        BitConverter.GetBytes((short)1).CopyTo(bitmap, 26); // color planes
        BitConverter.GetBytes((short)24).CopyTo(bitmap, 28); // bits per pixel
        BitConverter.GetBytes(pixelDataSize).CopyTo(bitmap, 34);

        for (int row = 0; row < height; row++)
        {
            int sourceRowStart = row * width * 3;
            int destinationRowStart = 54 + ((height - 1 - row) * rowSize);
            Array.Copy(bgr, sourceRowStart, bitmap, destinationRowStart, width * 3);
        }

        return bitmap;
    }
#endif

    private void HandleCallEvent(CallClientEvent callEvent)
    {
        this.eventLog.Insert(0, $"{callEvent.OccurredDate:HH:mm:ss} {callEvent.EventType}");

        switch (callEvent.EventType)
        {
            case "IncomingCall":
                this.incomingCallHandle = callEvent.Handle;
                this.currentCallIncludesVideo = callEvent.IncludesVideo;
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
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        this.RegistrationStatusLabel.Text = "Registering...";
        this.RegistrationStatusLabel.TextColor = Colors.Orange;

        try
        {
            await this.callClient.RegisterAsync(new SipCredentials
            {
                ClientId = Guid.NewGuid(),
                Extension = this.ExtensionEntry.Text ?? string.Empty,
                Password = this.PasswordEntry.Text ?? string.Empty
            });

            this.RegistrationStatusLabel.Text = $"Registered as {this.ExtensionEntry.Text}";
            this.RegistrationStatusLabel.TextColor = Colors.Green;
        }
        catch (Exception exception)
        {
            this.RegistrationStatusLabel.Text = $"Registration failed: {exception.Message}";
            this.RegistrationStatusLabel.TextColor = Colors.Red;
        }
    }

    private async void OnAudioCallClicked(object? sender, EventArgs e) =>
        await this.PlaceCallAsync(includeVideo: false);

    private async void OnVideoCallClicked(object? sender, EventArgs e) =>
        await this.PlaceCallAsync(includeVideo: true);

    private async Task PlaceCallAsync(bool includeVideo)
    {
        string targetExtension = this.TargetExtensionEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(targetExtension))
        {
            return;
        }

        this.currentCallIncludesVideo = includeVideo;
        this.ShowDialing(targetExtension);

        try
        {
            this.currentCallHandle = await this.callClient.PlaceCallAsync(targetExtension, includeVideo);
        }
        catch (Exception exception)
        {
            this.eventLog.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} Call failed: {exception.Message}");
            this.ShowCallFailed();
        }
    }

    private async void OnHangupClicked(object? sender, EventArgs e)
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
                this.eventLog.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} Hangup failed: {exception.Message}");
            }
        }

        this.currentCallHandle = null;
        this.incomingCallHandle = null;
        this.ShowSetup();
    }

    private async void OnAnswerClicked(object? sender, EventArgs e)
    {
        if (this.incomingCallHandle is not null)
        {
            try
            {
                await this.callClient.AnswerCallAsync(this.incomingCallHandle);
                this.currentCallHandle = this.incomingCallHandle;
                this.ShowActive();
            }
            catch (Exception exception)
            {
                this.eventLog.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} Answer failed: {exception.Message}");
                this.ShowCallFailed();
            }
        }
    }

    private async void OnDeclineClicked(object? sender, EventArgs e)
    {
        if (this.incomingCallHandle is not null)
        {
            try
            {
                await this.callClient.HangupAsync(this.incomingCallHandle);
            }
            catch (Exception exception)
            {
                this.eventLog.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} Decline failed: {exception.Message}");
            }

            this.incomingCallHandle = null;
        }

        this.ShowSetup();
    }

    private async void OnMuteClicked(object? sender, EventArgs e)
    {
        try
        {
            if (this.isMuted)
            {
                await this.callClient.UnmuteAsync();
            }
            else
            {
                await this.callClient.MuteAsync();
            }

            this.isMuted = !this.isMuted;
            this.MuteButton.Text = this.isMuted ? "🔇" : "🎙";
            this.MuteButton.BackgroundColor = this.isMuted ? Color.FromArgb("#5B4CE0") : Color.FromArgb("#2A2E3F");
        }
        catch (Exception exception)
        {
            this.eventLog.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} Mute failed: {exception.Message}");
        }
    }

    private async void OnVideoClicked(object? sender, EventArgs e)
    {
        try
        {
            if (this.isVideoMuted)
            {
                await this.callClient.UnmuteVideoAsync();
            }
            else
            {
                await this.callClient.MuteVideoAsync();
            }

            this.isVideoMuted = !this.isVideoMuted;
            this.VideoButton.Text = this.isVideoMuted ? "🚫" : "📷";
            this.VideoButton.BackgroundColor = this.isVideoMuted ? Color.FromArgb("#2A2E3F") : Color.FromArgb("#5B4CE0");
            this.LocalCameraView.IsVisible = !this.isVideoMuted;
        }
        catch (Exception exception)
        {
            this.eventLog.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} Video toggle failed: {exception.Message}");
        }
    }

    // Always compiled (not #if ANDROID) so the XAML button's Clicked handler resolves on every
    // platform — androidVideoEndPoint is simply null on Windows, matching the same
    // null-check-and-no-op pattern used throughout this file for platform-specific endpoints.
    // The button itself is only made visible on Android (see OnAppearing/ShowSetup).
    private async void OnSwitchCameraClicked(object? sender, EventArgs e)
    {
        try
        {
#if ANDROID
            if (this.androidVideoEndPoint is not null)
            {
                await this.androidVideoEndPoint.SwitchCameraAsync();
            }
#else
            await Task.CompletedTask;
#endif
        }
        catch (Exception exception)
        {
            this.eventLog.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} Camera switch failed: {exception.Message}");
        }
    }

    private void OnSpeakerClicked(object? sender, EventArgs e)
    {
        // UI-only toggle — SIPSorceryMedia.Windows's WindowsAudioEndPoint has no
        // speaker/earpiece routing concept (that's a mobile-specific dichotomy), and there's
        // no audio device to route on Android yet either (see MauiProgram.cs). Flips the
        // button state so the screen matches the reference design; doesn't change real
        // audio output.
        this.isSpeakerOn = !this.isSpeakerOn;
        this.SpeakerButton.BackgroundColor = this.isSpeakerOn ? Color.FromArgb("#5B4CE0") : Color.FromArgb("#2A2E3F");
    }

    private void ShowSetup()
    {
        this.StopCallTimer();
        this.StopTone();
        this.SetupView.IsVisible = true;
        this.InCallView.IsVisible = false;
        this.CallFailedLabel.IsVisible = false;
        this.isMuted = false;
        this.isSpeakerOn = false;
        this.isVideoMuted = false;
        this.currentCallIncludesVideo = false;
        this.RemoteVideoImage.IsVisible = false;
        this.RemoteVideoImage.Source = null;
        this.LocalCameraView.IsVisible = false;
        this.LocalVideoImage.IsVisible = false;
        this.LocalVideoImage.Source = null;
        this.VideoButton.Text = "📷";
        this.VideoButton.BackgroundColor = Color.FromArgb("#2A2E3F");
    }

    private void ShowDialing(string targetExtension)
    {
        this.SetupView.IsVisible = false;
        this.InCallView.IsVisible = true;
        this.CallFailedLabel.IsVisible = false;

        this.CallAvatarLabel.Text = AvatarInitials(targetExtension);
        this.CallStateLabel.Text = "Calling";
        this.CallTargetLabel.Text = targetExtension;
        this.CallTimerLabel.IsVisible = false;

        this.DialingButtons.IsVisible = true;
        this.ActiveCallButtons.IsVisible = false;
        this.IncomingCallButtons.IsVisible = false;

        this.PlayTone(ToneGenerator.CreateRingbackTone());
    }

    private void ShowIncoming(string callerExtension)
    {
        this.SetupView.IsVisible = false;
        this.InCallView.IsVisible = true;
        this.CallFailedLabel.IsVisible = false;

        this.CallAvatarLabel.Text = AvatarInitials(callerExtension);
        this.CallStateLabel.Text = "Incoming call";
        this.CallTargetLabel.Text = string.IsNullOrEmpty(callerExtension) ? "Unknown" : callerExtension;
        this.CallTimerLabel.IsVisible = false;

        this.DialingButtons.IsVisible = false;
        this.ActiveCallButtons.IsVisible = false;
        this.IncomingCallButtons.IsVisible = true;

        this.PlayTone(ToneGenerator.CreateRingTone());
    }

    private void ShowActive()
    {
        this.StopTone();
        this.SetupView.IsVisible = false;
        this.InCallView.IsVisible = true;
        this.CallFailedLabel.IsVisible = false;

        this.CallStateLabel.Text = "In call with";
        this.CallTimerLabel.IsVisible = true;

        this.DialingButtons.IsVisible = false;
        this.IncomingCallButtons.IsVisible = false;
        this.ActiveCallButtons.IsVisible = true;

#if WINDOWS
        this.LocalCameraView.IsVisible = this.windowsVideoEndPoint is not null && this.currentCallIncludesVideo;
#elif ANDROID
        // LocalCameraView stays hidden on Android — AndroidCameraXVideoEndPoint has no native
        // preview surface bound to it (see its own class comment), so it would just show a blank
        // box, which is exactly the "self preview doesn't work" symptom this replaces.
        // LocalVideoImage.IsVisible is instead set as frames actually arrive — see
        // OnLocalFrameReady — so it only appears once there's really something to show.
        this.LocalCameraView.IsVisible = false;
#endif

        this.StartCallTimer();
    }

    private void ShowCallFailed()
    {
        this.StopCallTimer();
        this.StopTone();
        this.SetupView.IsVisible = true;
        this.InCallView.IsVisible = false;
        this.CallFailedLabel.Text = "Call ended unexpectedly — see event log below.";
        this.CallFailedLabel.IsVisible = true;
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
            this.eventLog.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} Tone playback failed: {exception.Message}");
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
        this.CallTimerLabel.Text = "00:00";

        this.callTimer = this.Dispatcher.CreateTimer();
        this.callTimer.Interval = TimeSpan.FromSeconds(1);
        this.callTimer.Tick += (_, _) =>
        {
            TimeSpan elapsed = DateTimeOffset.Now - this.callAnsweredAt;
            this.CallTimerLabel.Text = elapsed.ToString(elapsed.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
        };

        this.callTimer.Start();
    }

    private void StopCallTimer()
    {
        this.callTimer?.Stop();
        this.callTimer = null;
    }

    private static string AvatarInitials(string extension) =>
        string.IsNullOrEmpty(extension) ? "?" : extension[..Math.Min(2, extension.Length)];
}
