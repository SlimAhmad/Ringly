using Microsoft.AspNetCore.Components;
using Ringly.Samples.BlazorHybrid.ViewServices.Agents;

namespace Ringly.Samples.BlazorHybrid.Components.Cores;

public partial class AgentConsole : ComponentBase, IDisposable
{
    [Inject]
    private IAgentConsoleViewService ViewService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        this.ViewService.StateChanged += this.OnViewServiceStateChanged;
        await this.ViewService.InitializeAsync();
    }

    private void OnViewServiceStateChanged() => this.InvokeAsync(this.StateHasChanged);

    private Task OnToggleAvailabilityClickedAsync() => this.ViewService.ToggleAvailabilityAsync().AsTask();

    private Task OnClaimClickedAsync(string channelId) => this.ViewService.ClaimAsync(channelId).AsTask();

    public void Dispose() => this.ViewService.StateChanged -= this.OnViewServiceStateChanged;
}
