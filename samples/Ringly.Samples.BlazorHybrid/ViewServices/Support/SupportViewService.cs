using Ringly.Client.Abstractions;
using Ringly.Client.Abstractions.Models;
using Ringly.Samples.BlazorHybrid.Brokers.Apis;

namespace Ringly.Samples.BlazorHybrid.ViewServices.Support;

public sealed class SupportViewService : ISupportViewService
{
    private readonly ISupportApiBroker supportApiBroker;
    private readonly ICallClient callClient;

    public event Action? StateChanged;

    public string QueueName { get; set; } = string.Empty;
    public string StatusMessage { get; private set; } = string.Empty;
    public string StatusMessageColorClass { get; private set; } = string.Empty;
    public bool IsBusy { get; private set; }

    public SupportViewService(ISupportApiBroker supportApiBroker, ICallClient callClient)
    {
        this.supportApiBroker = supportApiBroker;
        this.callClient = callClient;
    }

    public async ValueTask RequestSupportAsync()
    {
        if (string.IsNullOrWhiteSpace(this.QueueName))
        {
            return;
        }

        this.IsBusy = true;
        this.StatusMessage = "Requesting support...";
        this.StatusMessageColorClass = "text-amber-400";
        this.OnStateChanged();

        try
        {
            // A fresh identity per support request — this app's own manual "Register" flow
            // (CallViewService/CallScreen) is a separate, independent path against the same
            // shared ICallClient; using both in the same session re-registers the same SIP UA
            // under a different identity, which is fine for a sample app but worth knowing.
            Guid clientId = Guid.NewGuid();
            SipCredentials credentials = await this.supportApiBroker.PostCredentialsAsync(clientId);
            await this.callClient.RegisterAsync(credentials);
            await this.supportApiBroker.PostSupportRouteAsync(clientId, this.QueueName);

            this.StatusMessage = $"Routed to \"{this.QueueName}\" as {credentials.Extension} — wait for the call.";
            this.StatusMessageColorClass = "text-emerald-400";
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Support request failed: {exception.Message}";
            this.StatusMessageColorClass = "text-red-400";
        }

        this.IsBusy = false;
        this.OnStateChanged();
    }

    private void OnStateChanged() => this.StateChanged?.Invoke();

    public void Dispose()
    {
    }
}
