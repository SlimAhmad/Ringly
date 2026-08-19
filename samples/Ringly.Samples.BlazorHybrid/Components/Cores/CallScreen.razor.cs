using Microsoft.AspNetCore.Components;
using Ringly.Samples.BlazorHybrid.ViewServices.Calls;

namespace Ringly.Samples.BlazorHybrid.Components.Cores;

public partial class CallScreen : ComponentBase, IDisposable
{
    // The single dependency this Core Component integrates with — see CallScreen.razor's own
    // comment. All action handlers below only delegate to it and never touch ICallClient,
    // IAudioManager, or IVideoFramePreviewSource directly.
    [Inject]
    private ICallViewService ViewService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        this.ViewService.StateChanged += this.OnViewServiceStateChanged;
        await this.ViewService.InitializeAsync();
    }

    private void OnViewServiceStateChanged() => this.InvokeAsync(this.StateHasChanged);

    private Task OnRegisterClickedAsync() => this.ViewService.RegisterAsync().AsTask();

    private Task OnAudioCallClickedAsync() => this.ViewService.PlaceAudioCallAsync().AsTask();

    private Task OnVideoCallClickedAsync() => this.ViewService.PlaceVideoCallAsync().AsTask();

    private Task OnHangupClickedAsync() => this.ViewService.HangupAsync().AsTask();

    private Task OnAnswerClickedAsync() => this.ViewService.AnswerAsync().AsTask();

    private Task OnDeclineClickedAsync() => this.ViewService.DeclineAsync().AsTask();

    private Task OnMuteClickedAsync() => this.ViewService.ToggleMuteAsync().AsTask();

    private Task OnVideoToggleClickedAsync() => this.ViewService.ToggleVideoMuteAsync().AsTask();

    private Task OnSwitchCameraClickedAsync() => this.ViewService.SwitchCameraAsync().AsTask();

    private static string AvatarInitials(string extensionValue) =>
        string.IsNullOrEmpty(extensionValue) ? "?" : extensionValue[..Math.Min(2, extensionValue.Length)];

    public void Dispose() => this.ViewService.StateChanged -= this.OnViewServiceStateChanged;
}
