namespace Ringly.Samples.BlazorServer.Video;

// Exposes decoded remote video frames as ready-to-render <img> data: URIs, so the view service
// (and, through it, CallScreen) never has to know about SIPSorceryMedia.Windows.WindowsVideoEndPoint
// or raw BGR/stride handling directly — mirrors Ringly.Samples.BlazorHybrid's own
// IVideoFramePreviewSource, minus the per-platform #if branching (this project only ever targets
// Windows).
public interface IVideoFramePreviewSource
{
    event Action<string>? RemoteFrameDataUriReady;
}
