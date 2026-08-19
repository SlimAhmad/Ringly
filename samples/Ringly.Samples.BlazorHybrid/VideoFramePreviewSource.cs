namespace Ringly.Samples.BlazorHybrid;

// Normalizes CustomWindowsVideoEndPoint/AndroidCameraXVideoEndPoint's platform-specific frame
// events behind one cross-platform type Home.razor can @inject directly — Razor's top-level
// directives (@inject) don't support #if/#elif preprocessor conditionals the way a plain .cs
// @code block does, so the platform branching has to live in ordinary C# files instead, one per
// platform, each registered as the sole IVideoFramePreviewSource implementation in MauiProgram.cs.
public interface IVideoFramePreviewSource
{
    // Raw decoded remote frames (BGR24) for a UI layer to render — mirrors
    // Ringly.Samples.Maui.CallPage.xaml.cs's own OnDecodedFrameReady/BuildBitmap pattern, just
    // rendered as an <img> data URI here instead of a MAUI ImageSource.
    event Action<int, int, byte[]>? DecodedFrameReady;

    // Raw locally-captured frames (BGR24) for a self-preview PIP — only ever raised on Android
    // (see AndroidCameraXVideoEndPoint's own comment); Windows renders its local preview through
    // MainPage.xaml's native CameraView overlay instead, so this event never fires there.
    event Action<int, int, byte[]>? LocalFrameReady;

    Task SwitchCameraAsync();
}

#if WINDOWS
public sealed class WindowsVideoFramePreviewSource : IVideoFramePreviewSource
{
    private readonly Ringly.Samples.Maui.Platforms.Windows.CustomWindowsVideoEndPoint endpoint;

    public WindowsVideoFramePreviewSource(Ringly.Samples.Maui.Platforms.Windows.CustomWindowsVideoEndPoint endpoint) =>
        this.endpoint = endpoint;

    public event Action<int, int, byte[]>? DecodedFrameReady
    {
        add => this.endpoint.DecodedFrameReady += value;
        remove => this.endpoint.DecodedFrameReady -= value;
    }

    public event Action<int, int, byte[]>? LocalFrameReady
    {
        add { }
        remove { }
    }

    // No camera-switch concept on Windows in this sample (single built-in webcam is the common
    // case) — mirrors CallPage.xaml.cs's OnSwitchCameraClicked #else branch.
    public Task SwitchCameraAsync() => Task.CompletedTask;
}
#elif ANDROID
public sealed class AndroidVideoFramePreviewSource : IVideoFramePreviewSource
{
    private readonly Ringly.Samples.Maui.Platforms.Android.IAndroidVideoCaptureEndPoint endpoint;

    public AndroidVideoFramePreviewSource(Ringly.Samples.Maui.Platforms.Android.IAndroidVideoCaptureEndPoint endpoint) =>
        this.endpoint = endpoint;

    public event Action<int, int, byte[]>? DecodedFrameReady
    {
        add => this.endpoint.DecodedFrameReady += value;
        remove => this.endpoint.DecodedFrameReady -= value;
    }

    public event Action<int, int, byte[]>? LocalFrameReady
    {
        add => this.endpoint.LocalFrameReady += value;
        remove => this.endpoint.LocalFrameReady -= value;
    }

    public Task SwitchCameraAsync() => this.endpoint.SwitchCameraAsync();
}
#endif
