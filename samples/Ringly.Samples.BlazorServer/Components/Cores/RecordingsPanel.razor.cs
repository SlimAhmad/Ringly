using Microsoft.AspNetCore.Components;
using Ringly.Samples.BlazorServer.ViewServices.Recordings;

namespace Ringly.Samples.BlazorServer.Components.Cores;

public partial class RecordingsPanel : ComponentBase, IDisposable
{
    [Inject]
    private IRecordingViewService ViewService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        this.ViewService.StateChanged += this.OnViewServiceStateChanged;
        await this.ViewService.InitializeAsync();
    }

    private void OnViewServiceStateChanged() => this.InvokeAsync(this.StateHasChanged);

    private Task OnCreateClickedAsync() => this.ViewService.CreateRecordingAsync().AsTask();

    private Task OnPauseClickedAsync(string recordingName) => this.ViewService.PauseAsync(recordingName).AsTask();

    private Task OnUnpauseClickedAsync(string recordingName) => this.ViewService.UnpauseAsync(recordingName).AsTask();

    private Task OnStopClickedAsync(string recordingName) => this.ViewService.StopAsync(recordingName).AsTask();

    private Task OnCancelClickedAsync(string recordingName) => this.ViewService.CancelAsync(recordingName).AsTask();

    private Task OnRemoveClickedAsync(string recordingName) => this.ViewService.RemoveAsync(recordingName).AsTask();

    public void Dispose() => this.ViewService.StateChanged -= this.OnViewServiceStateChanged;
}
