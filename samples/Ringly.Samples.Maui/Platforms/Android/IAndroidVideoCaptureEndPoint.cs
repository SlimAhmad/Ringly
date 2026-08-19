namespace Ringly.Samples.Maui.Platforms.Android;

// Shared surface between AndroidVideoEndPoint (drives capture through Shiny.Maui.Controls.Camera's
// CameraView/IFrameAnalyzer) and AndroidCameraXVideoEndPoint (bypasses Shiny entirely, drives
// CameraX's ProcessCameraProvider/ImageAnalysis directly) — see #143 for why a second
// implementation exists: live testing showed Shiny's own analyzer callback never firing despite
// every documented precondition being satisfied, and the only way to confirm whether that's a
// genuine bug in Shiny's library (versus a mistake in how we drove it) is to compare against a
// minimal, direct-CameraX capture path built independently of it.
//
// CallPage depends on this interface, not either concrete type, so MauiProgram.cs's DI
// registration alone decides which implementation is active — nothing else needs to change to
// switch between them.
public interface IAndroidVideoCaptureEndPoint
{
    void AttachCameraView(Shiny.Maui.Controls.Camera.CameraView cameraView);

    void DetachCameraView();

    Task SwitchCameraAsync();

    event Action<int, int, byte[]>? DecodedFrameReady;

    // Raw locally-captured frames (BGR24, same format/shape as DecodedFrameReady — see
    // CallPage.xaml.cs's shared OnDecodedFrameReady/BuildBitmap for why), for a self-preview PIP to
    // render. AndroidVideoEndPoint (Shiny CameraView path) never raises this — CameraView is its
    // own native preview surface and shows the live feed on its own once attached, so there's
    // nothing for this class to convert/publish. AndroidCameraXVideoEndPoint (no native preview
    // surface at all — see its own class comment) raises it as the one way it can show a
    // self-preview.
    event Action<int, int, byte[]>? LocalFrameReady;
}
