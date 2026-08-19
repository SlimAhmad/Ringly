namespace Ringly.Samples.BlazorHybrid.ViewServices.Support;

// The single dependency SupportPanel.razor (the Core Component) integrates with — mirrors
// ICallViewService's own role for CallScreen. Owns the "cold support" flow: provision fresh SIP
// credentials via Ringly.Samples.WebApi, register this app's ICallClient with them, then route
// to a support queue — the customer's own registered extension then rings like any other
// incoming call once an agent/queue member answers, handled by the existing CallScreen exactly
// like any other incoming call.
public interface ISupportViewService : IDisposable
{
    event Action? StateChanged;

    string QueueName { get; set; }
    string StatusMessage { get; }
    string StatusMessageColorClass { get; }
    bool IsBusy { get; }

    ValueTask RequestSupportAsync();
}
