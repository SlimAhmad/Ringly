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

    public CallPage(ICallClient callClient, IAudioManager audioManager)
    {
        InitializeComponent();
        this.callClient = callClient;
        this.audioManager = audioManager;
        this.EventLogView.ItemsSource = this.eventLog;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        this.eventSubscription = this.callClient.StreamEvents().Subscribe(callEvent =>
            this.Dispatcher.Dispatch(() => this.HandleCallEvent(callEvent)));

#if ANDROID
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
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        this.eventSubscription?.Dispose();
        this.StopCallTimer();
        this.StopTone();
    }

    private void HandleCallEvent(CallClientEvent callEvent)
    {
        this.eventLog.Insert(0, $"{callEvent.OccurredDate:HH:mm:ss} {callEvent.EventType}");

        switch (callEvent.EventType)
        {
            case "IncomingCall":
                this.incomingCallHandle = callEvent.Handle;
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

    private async void OnCallClicked(object? sender, EventArgs e)
    {
        string targetExtension = this.TargetExtensionEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(targetExtension))
        {
            return;
        }

        this.ShowDialing(targetExtension);

        try
        {
            this.currentCallHandle = await this.callClient.PlaceCallAsync(targetExtension);
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
