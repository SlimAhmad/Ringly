using System.Collections.ObjectModel;
using Ringly.Client.Abstractions;
using Ringly.Client.Abstractions.Models;

namespace Ringly.Samples.Maui;

public partial class CallPage : ContentPage
{
    private readonly ICallClient callClient;
    private readonly ObservableCollection<string> eventLog = [];
    private IDisposable? eventSubscription;
    private CallHandle? currentCallHandle;
    private CallHandle? incomingCallHandle;

    public CallPage(ICallClient callClient)
    {
        InitializeComponent();
        this.callClient = callClient;
        this.EventLogView.ItemsSource = this.eventLog;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        this.eventSubscription = this.callClient.StreamEvents().Subscribe(callEvent =>
            this.Dispatcher.Dispatch(() => this.HandleCallEvent(callEvent)));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        this.eventSubscription?.Dispose();
    }

    private void HandleCallEvent(CallClientEvent callEvent)
    {
        this.eventLog.Insert(0, $"{callEvent.OccurredDate:HH:mm:ss} {callEvent.EventType}");

        switch (callEvent.EventType)
        {
            case "IncomingCall":
                this.incomingCallHandle = callEvent.Handle;
                this.IncomingCallLabel.IsVisible = true;
                this.IncomingCallButtons.IsVisible = true;
                break;

            case "CallAnswered":
                this.IncomingCallLabel.IsVisible = false;
                this.IncomingCallButtons.IsVisible = false;
                break;

            case "CallHungup":
            case "CallFailed":
                this.IncomingCallLabel.IsVisible = false;
                this.IncomingCallButtons.IsVisible = false;
                this.currentCallHandle = null;
                this.incomingCallHandle = null;
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

        this.currentCallHandle = await this.callClient.PlaceCallAsync(targetExtension);
    }

    private async void OnHangupClicked(object? sender, EventArgs e)
    {
        if (this.currentCallHandle is not null)
        {
            await this.callClient.HangupAsync(this.currentCallHandle);
            this.currentCallHandle = null;
        }
    }

    private async void OnAnswerClicked(object? sender, EventArgs e)
    {
        if (this.incomingCallHandle is not null)
        {
            await this.callClient.AnswerCallAsync(this.incomingCallHandle);
            this.currentCallHandle = this.incomingCallHandle;
            this.IncomingCallLabel.IsVisible = false;
            this.IncomingCallButtons.IsVisible = false;
        }
    }

    private async void OnDeclineClicked(object? sender, EventArgs e)
    {
        if (this.incomingCallHandle is not null)
        {
            await this.callClient.HangupAsync(this.incomingCallHandle);
            this.incomingCallHandle = null;
        }

        this.IncomingCallLabel.IsVisible = false;
        this.IncomingCallButtons.IsVisible = false;
    }
}
