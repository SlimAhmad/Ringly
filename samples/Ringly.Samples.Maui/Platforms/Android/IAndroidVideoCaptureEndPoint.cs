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

    event Action<int, int, byte[]>? DecodedFrameReady;
}
