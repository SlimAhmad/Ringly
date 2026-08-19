using Microsoft.AspNetCore.Components;
using Ringly.Samples.BlazorHybrid.ViewServices.Support;

namespace Ringly.Samples.BlazorHybrid.Components.Cores;

public partial class SupportPanel : ComponentBase, IDisposable
{
    [Inject]
    private ISupportViewService ViewService { get; set; } = default!;

    protected override void OnInitialized() =>
        this.ViewService.StateChanged += this.OnViewServiceStateChanged;

    private void OnViewServiceStateChanged() => this.InvokeAsync(this.StateHasChanged);

    private Task OnRequestSupportClickedAsync() => this.ViewService.RequestSupportAsync().AsTask();

    public void Dispose() => this.ViewService.StateChanged -= this.OnViewServiceStateChanged;
}
