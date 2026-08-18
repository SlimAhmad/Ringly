using System.Net;
using Microsoft.Extensions.Logging;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera;
using SIPSorceryMedia.Abstractions;
using Vpx.Net;

namespace Ringly.Samples.Maui.Platforms.Android;

// Video counterpart to AndroidAudioEndPoint.cs — same reasons for hand-rolling apply (no
// cross-platform SIPSorceryMedia video package exists), and shares CustomWindowsVideoEndPoint.cs's
// choice of capture library (Shiny.Maui.Controls.Camera's CameraView + IFrameAnalyzer) and codec
// (SIPSorcery.VP8 — pure managed, works unmodified on both platforms). The one real platform
// difference: Android's camera frames arrive as CameraX's YUV_420_888 (three separate Y/U/V
// planes, each with its own row/pixel stride — confirmed via AndroidCameraFrame's own source,
// which hands back the raw IImageProxy with zero copies), not a ready-made packed buffer like
// Windows' BGRA SoftwareBitmap — so this class repacks the three planes into I420 (VP8Codec's own
// native input layout) before handing frames to the encoder, instead of converting color spaces.
//
// Same lifecycle model as the Windows endpoint: camera capture is UI-bound (CameraView must live
// in a page's visual tree), so this class doesn't own a capture device outright — whatever page
// hosts the call UI calls AttachCameraView(cameraView)/DetachCameraView() around its own
// lifecycle, and this class only starts/stops the attached view's capture session in response to
// real SipSorceryCallClient connection-state changes (StartVideo/CloseVideo).
//
// Same first-pass scoping as Windows: this covers the capture -> encode -> RTP -> decode pipeline
// itself; rendering decoded remote frames to an on-screen surface is left to a following piece of
// work (DecodedFrameReady exposes the raw decoded samples for that to consume, same contract as
// CustomWindowsVideoEndPoint).
public sealed class AndroidVideoEndPoint : IVideoSource, IVideoSink, IFrameAnalyzer, IAndroidVideoCaptureEndPoint, IDisposable
{
    // VP8Codec.EncodeVideo requires both dimensions to be exact multiples of 16 — no
    // padding/cropping support in the foundation encoder (see VP8Codec.cs). Frames whose native
    // resolution doesn't comply are logged and skipped outright, same as the Windows endpoint —
    // the vast majority of common camera resolutions (640x480, 1280x720, 1920x1080) already are.
    private const int RequiredDimensionMultiple = 16;

    // Same nominal-30fps RTP timestamp approximation as CustomWindowsVideoEndPoint — frames arrive
    // whenever CameraX's analyzer pipeline delivers them, no timer of our own pacing capture.
    private const uint VideoClockRateHz = 90000;
    private const uint AssumedFrameRate = 30;
    private const uint AssumedDurationRtpUnits = VideoClockRateHz / AssumedFrameRate;

    // Not static readonly — confirmed live (see AndroidAudioEndPoint/CustomWindowsAudioEndPoint)
    // that a static readonly logger field can permanently capture SIPSorcery's no-op default
    // logger if this class is constructed (as it is, in MauiProgram.cs) before
    // SIPSorcery.LogFactory.Set() wires up the real ILoggerFactory.
    private static ILogger Logger => SIPSorcery.LogFactory.CreateLogger<AndroidVideoEndPoint>();

    private readonly VP8Codec videoCodec = new();
    private readonly MediaFormatManager<VideoFormat> sourceFormatManager;
    private readonly MediaFormatManager<VideoFormat> sinkFormatManager;

    private CameraView? attachedCameraView;
    private bool isSourcePaused;
    private bool isSinkPaused;

    private int framesEncodedSinceLog;
    private int framesSkippedSinceLog;
    private DateTime lastEncodeLogAt = DateTime.MinValue;

    private int framesDecodedSinceLog;
    private DateTime lastDecodeLogAt = DateTime.MinValue;
    private DateTime lastDecodeSkipLogAt = DateTime.MinValue;

    // Diagnostic only: confirmed live that "Encoded N video frames" never appeared at all in a
    // real call log despite StartVideo() completing successfully — these narrow down whether
    // CameraX/Shiny's analyzer pipeline is calling into this class at all (WantsFrame/AnalyzeAsync
    // never invoked = a binding/use-case problem upstream of this code) versus being invoked but
    // rejected by a condition inside AnalyzeAsync itself (a logic bug in this file).
    private bool hasLoggedFirstWantsFrameCall;
    private bool hasLoggedFirstAnalyzeCall;
    private int wantsFrameTrueCountSinceLog;
    private int wantsFrameFalseCountSinceLog;
    private DateTime lastWantsFrameLogAt = DateTime.MinValue;

    public event EncodedSampleDelegate? OnVideoSourceEncodedSample;
    public event RawVideoSampleDelegate? OnVideoSourceRawSample;
    public event RawVideoSampleFasterDelegate? OnVideoSourceRawSampleFaster;
    public event SourceErrorDelegate? OnVideoSourceError;
    public event VideoSinkSampleDecodedDelegate? OnVideoSinkDecodedSample;
    public event VideoSinkSampleDecodedFasterDelegate? OnVideoSinkDecodedSampleFaster;

    // Raw decoded remote frames (BGR, per VP8Codec.DecodeVideo's internal conversion), for a UI
    // layer to render — this class deliberately doesn't own any rendering surface itself, same as
    // CustomWindowsVideoEndPoint.
    public event Action<int, int, byte[]>? DecodedFrameReady;

    public AndroidVideoEndPoint()
    {
        this.sourceFormatManager = new MediaFormatManager<VideoFormat>(this.videoCodec.SupportedFormats);
        this.sinkFormatManager = new MediaFormatManager<VideoFormat>(this.videoCodec.SupportedFormats);
    }

    // Called by the call page once its CameraView is available (e.g. OnAppearing) — wires this
    // endpoint up as the view's frame analyzer so captured frames flow into AnalyzeAsync below.
    public void AttachCameraView(CameraView cameraView)
    {
        this.attachedCameraView = cameraView;
        cameraView.Analyzer = this;
        Logger.LogInformation("AttachCameraView called, Analyzer assigned to this instance.");
    }

    public void DetachCameraView()
    {
        if (this.attachedCameraView is not null)
        {
            this.attachedCameraView.Analyzer = null;
            this.attachedCameraView = null;
        }
    }

    // IFrameAnalyzer

    public string Id => nameof(AndroidVideoEndPoint);

    public bool WantsFrame()
    {
        if (!this.hasLoggedFirstWantsFrameCall)
        {
            this.hasLoggedFirstWantsFrameCall = true;
            Logger.LogInformation("WantsFrame called for the first time — the analyzer pipeline is reaching this class.");
        }

        bool wantsFrame = !this.isSourcePaused && this.HasEncodedVideoSubscribers();

        if (wantsFrame)
        {
            this.wantsFrameTrueCountSinceLog++;
        }
        else
        {
            this.wantsFrameFalseCountSinceLog++;
        }

        if (DateTime.UtcNow - this.lastWantsFrameLogAt > TimeSpan.FromSeconds(2))
        {
            Logger.LogInformation(
                "WantsFrame polled {TrueCount} true / {FalseCount} false in the last ~2s (isSourcePaused={IsSourcePaused}, hasSubscribers={HasSubscribers}).",
                this.wantsFrameTrueCountSinceLog,
                this.wantsFrameFalseCountSinceLog,
                this.isSourcePaused,
                this.HasEncodedVideoSubscribers());

            this.lastWantsFrameLogAt = DateTime.UtcNow;
            this.wantsFrameTrueCountSinceLog = 0;
            this.wantsFrameFalseCountSinceLog = 0;
        }

        return wantsFrame;
    }

    public ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        if (!this.hasLoggedFirstAnalyzeCall)
        {
            this.hasLoggedFirstAnalyzeCall = true;
            Logger.LogInformation(
                "AnalyzeAsync called for the first time — frame type {FrameType}, {Width}x{Height}.",
                frame.GetType().Name,
                frame.Width,
                frame.Height);
        }

        if (frame is not AndroidCameraFrame androidFrame)
        {
            return ValueTask.FromResult<IReadOnlyList<OverlayBox>?>(null);
        }

        if (this.isSourcePaused || this.sourceFormatManager.SelectedFormat.IsEmpty())
        {
            return ValueTask.FromResult<IReadOnlyList<OverlayBox>?>(null);
        }

        if (androidFrame.Width % RequiredDimensionMultiple != 0 || androidFrame.Height % RequiredDimensionMultiple != 0)
        {
            this.framesSkippedSinceLog++;
            this.LogEncodeSummaryIfDue();
            return ValueTask.FromResult<IReadOnlyList<OverlayBox>?>(null);
        }

        try
        {
            byte[] i420Sample = ConvertYuv420888ToI420(androidFrame.Proxy, androidFrame.Width, androidFrame.Height);

            byte[] encodedSample = this.videoCodec.EncodeVideo(
                androidFrame.Width,
                androidFrame.Height,
                i420Sample,
                VideoPixelFormatsEnum.I420,
                VideoCodecsEnum.VP8);

            this.OnVideoSourceEncodedSample?.Invoke(AssumedDurationRtpUnits, encodedSample);

            this.framesEncodedSinceLog++;
            this.LogEncodeSummaryIfDue();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to encode captured video frame.");
            this.OnVideoSourceError?.Invoke($"Video encode failed: {exception.Message}");
        }

        return ValueTask.FromResult<IReadOnlyList<OverlayBox>?>(null);
    }

    // CameraX hands YUV_420_888 back as three independent planes (Y, then interleaved-or-planar
    // U/V depending on device/vendor), each with its own RowStride (bytes per row, which can
    // exceed the plane's logical width) and PixelStride (bytes between consecutive samples in a
    // row — 1 for a fully planar chroma plane, 2 for a semi-planar one like NV21's interleaved
    // VU). I420 needs each plane packed tightly with no stride padding and no interleaving, so
    // this walks each plane's buffer with its own actual strides rather than assuming a fixed
    // layout — the only way to be correct across the different vendor camera HALs Android exposes
    // this same YUV_420_888 format through.
    private static byte[] ConvertYuv420888ToI420(AndroidX.Camera.Core.IImageProxy proxy, int width, int height)
    {
        var planes = proxy.GetPlanes()!;
        int chromaWidth = width / 2;
        int chromaHeight = height / 2;
        int ySize = width * height;
        int chromaSize = chromaWidth * chromaHeight;

        var i420 = new byte[ySize + (2 * chromaSize)];

        CopyPlane(planes[0], width, height, i420, 0);
        CopyPlane(planes[1], chromaWidth, chromaHeight, i420, ySize);
        CopyPlane(planes[2], chromaWidth, chromaHeight, i420, ySize + chromaSize);

        return i420;
    }

    private static void CopyPlane(AndroidX.Camera.Core.IImageProxyPlaneProxy plane, int planeWidth, int planeHeight, byte[] destination, int destinationOffset)
    {
        var buffer = plane.Buffer!;
        int rowStride = plane.RowStride;
        int pixelStride = plane.PixelStride;

        if (pixelStride == 1)
        {
            for (int row = 0; row < planeHeight; row++)
            {
                buffer.Position(row * rowStride);
                buffer.Get(destination, destinationOffset + (row * planeWidth), planeWidth);
            }

            return;
        }

        var rowBuffer = new byte[rowStride];

        for (int row = 0; row < planeHeight; row++)
        {
            buffer.Position(row * rowStride);
            int bytesAvailable = Math.Min(rowStride, buffer.Remaining());
            buffer.Get(rowBuffer, 0, bytesAvailable);

            for (int column = 0; column < planeWidth; column++)
            {
                destination[destinationOffset + (row * planeWidth) + column] = rowBuffer[column * pixelStride];
            }
        }
    }

    private void LogEncodeSummaryIfDue()
    {
        if (DateTime.UtcNow - this.lastEncodeLogAt <= TimeSpan.FromSeconds(2))
        {
            return;
        }

        Logger.LogInformation(
            "Encoded {FramesEncoded} video frames in the last ~2s ({FramesSkipped} skipped — non-multiple-of-16 resolution).",
            this.framesEncodedSinceLog,
            this.framesSkippedSinceLog);

        this.lastEncodeLogAt = DateTime.UtcNow;
        this.framesEncodedSinceLog = 0;
        this.framesSkippedSinceLog = 0;
    }

    // IVideoSource

    public List<VideoFormat> GetVideoSourceFormats() => this.sourceFormatManager.GetSourceFormats();

    public void SetVideoSourceFormat(VideoFormat videoFormat)
    {
        Logger.LogInformation("SetVideoSourceFormat called with {FormatName}.", videoFormat.FormatName);
        this.sourceFormatManager.SetSelectedFormat(videoFormat);
    }

    public void RestrictFormats(Func<VideoFormat, bool> filter)
    {
        this.sourceFormatManager.RestrictFormats(filter);
        this.sinkFormatManager.RestrictFormats(filter);
    }

    public void ExternalVideoSourceRawSample(
        uint durationMilliseconds, int width, int height, byte[] sample, VideoPixelFormatsEnum pixelFormat) =>
        throw new NotImplementedException();

    public void ExternalVideoSourceRawSampleFaster(uint durationMilliseconds, RawImage rawImage) =>
        throw new NotImplementedException();

    public void ForceKeyFrame() => this.videoCodec.ForceKeyFrame();

    public bool HasEncodedVideoSubscribers() => this.OnVideoSourceEncodedSample is not null;

    public bool IsVideoSourcePaused() => this.isSourcePaused;

    public Task StartVideo()
    {
        this.isSourcePaused = false;

        // Confirmed live via a real device crash: CameraView.StartAsync/StopAsync end up calling
        // AndroidX's LifecycleRegistry.SetCurrentState internally (CameraLifecycleOwner), which
        // throws IllegalStateException ("must be called on the main thread") unless invoked from
        // Android's UI thread. This method is called from SipSorceryCallClient's
        // mediaSession.onconnectionstatechange handler, which runs on SIPSorcery's own internal
        // event-dispatch thread — MainThread.InvokeOnMainThreadAsync marshals the call across.
        return this.attachedCameraView is null
            ? Task.CompletedTask
            : MainThread.InvokeOnMainThreadAsync(() => this.attachedCameraView.StartAsync());
    }

    public Task CloseVideo()
    {
        // Same main-thread requirement as StartVideo above.
        return this.attachedCameraView is null
            ? Task.CompletedTask
            : MainThread.InvokeOnMainThreadAsync(() => this.attachedCameraView.StopAsync());
    }

    public Task PauseVideo()
    {
        this.isSourcePaused = true;
        return Task.CompletedTask;
    }

    public Task ResumeVideo()
    {
        this.isSourcePaused = false;
        return Task.CompletedTask;
    }

    // IVideoSink

    public List<VideoFormat> GetVideoSinkFormats() => this.sinkFormatManager.GetSourceFormats();

    public void SetVideoSinkFormat(VideoFormat videoFormat)
    {
        Logger.LogInformation("SetVideoSinkFormat called with {FormatName}.", videoFormat.FormatName);
        this.sinkFormatManager.SetSelectedFormat(videoFormat);
    }

    [Obsolete("Use GotVideoFrame instead.")]
    public void GotVideoRtp(
        IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload) =>
        this.GotVideoFrame(remoteEndPoint, timestamp, payload, this.sinkFormatManager.SelectedFormat);

    public void GotVideoFrame(IPEndPoint remoteEndPoint, uint timestamp, byte[] payload, VideoFormat format)
    {
        if (this.isSinkPaused || format.IsEmpty())
        {
            if (DateTime.UtcNow - this.lastDecodeSkipLogAt > TimeSpan.FromSeconds(2))
            {
                this.lastDecodeSkipLogAt = DateTime.UtcNow;

                Logger.LogWarning(
                    "Video frame skipped: isSinkPaused={IsSinkPaused}, formatIsEmpty={FormatIsEmpty}.",
                    this.isSinkPaused,
                    format.IsEmpty());
            }

            return;
        }

        try
        {
            foreach (VideoSample sample in this.videoCodec.DecodeVideo(payload, VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.VP8))
            {
                this.DecodedFrameReady?.Invoke((int)sample.Width, (int)sample.Height, sample.Sample);
                this.framesDecodedSinceLog++;
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to decode received video frame.");
        }

        if (DateTime.UtcNow - this.lastDecodeLogAt > TimeSpan.FromSeconds(2))
        {
            Logger.LogInformation(
                "Decoded {FramesDecoded} video frames in the last ~2s.",
                this.framesDecodedSinceLog);

            this.lastDecodeLogAt = DateTime.UtcNow;
            this.framesDecodedSinceLog = 0;
        }
    }

    public Task StartVideoSink()
    {
        this.isSinkPaused = false;
        return Task.CompletedTask;
    }

    public Task CloseVideoSink()
    {
        return Task.CompletedTask;
    }

    public Task PauseVideoSink()
    {
        this.isSinkPaused = true;
        return Task.CompletedTask;
    }

    public Task ResumeVideoSink()
    {
        this.isSinkPaused = false;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        this.DetachCameraView();
        this.videoCodec.Dispose();
    }
}
