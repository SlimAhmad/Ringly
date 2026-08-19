using System.Net;
using Microsoft.Extensions.Logging;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera;
using SIPSorceryMedia.Abstractions;
using Vpx.Net;

namespace Ringly.Samples.Maui.Platforms.Windows;

// Video counterpart to CustomWindowsAudioEndPoint.cs — same reasons for hand-rolling apply
// (no cross-platform SIPSorceryMedia video package exists), plus two dependencies evaluated and
// chosen this session:
//   - Camera capture: Shiny.Maui.Controls.Camera's CameraView + IFrameAnalyzer. Confirmed via
//     direct source inspection (not just docs) that its CameraFrame hands back the real native
//     buffer with zero copies — WindowsCameraFrame.Bgra is a genuine managed copy of the BGRA8
//     SoftwareBitmap, not a derived/processed result — so this class implements IFrameAnalyzer
//     itself rather than hand-rolling Windows.Media.Capture.MediaCapture directly.
//   - Codec: SIPSorcery.VP8 (Vpx.Net.VP8Codec) — a pure managed C# VP8 codec, no native
//     binaries/PInvoke, same vendor/version family as the SIPSorcery/SIPSorceryMedia.Abstractions
//     packages already used by Ringly.Client.SipSorcery. Chosen over SIPSorceryMedia.Encoders'
//     VP8, whose native libvpx binaries are Windows-only (x86/x64 DLLs, no Android .so) — this
//     package works unmodified on both platforms.
//
// Unlike audio, camera capture is fundamentally UI-bound (CameraView is a MAUI View, must live in
// a page's visual tree) — this class can't own a capture device outright the way NAudio's
// WasapiCapture does. Instead, whatever page hosts the call UI calls AttachCameraView(cameraView)
// once the CameraView is available (e.g. OnAppearing) and DetachCameraView() when it's gone; this
// class only starts/stops the attached view's capture session in response to real
// SipSorceryCallClient connection-state changes (StartVideo/CloseVideo), and receives frames via
// the CameraView's Analyzer property (set to `this`).
//
// This first pass focuses on getting the capture -> encode -> RTP -> decode pipeline itself
// genuinely working (mirrors this session's audio approach: build the pipeline with diagnostics
// first, confirm real non-zero data is flowing, THEN wire up user-visible playback) — rendering
// decoded remote frames to an actual on-screen surface is deliberately left to a following piece
// of work; DecodedFrameReady exposes the raw decoded samples for that to consume.
public sealed class CustomWindowsVideoEndPoint : IVideoSource, IVideoSink, IFrameAnalyzer, IDisposable
{
    // VP8Codec.EncodeVideo requires both dimensions to be exact multiples of 16 — no
    // padding/cropping support in the foundation encoder (see VP8Codec.cs). Rather than attempt
    // error-prone stride-aware cropping of a captured frame that doesn't already satisfy this,
    // frames whose native resolution doesn't comply are logged and skipped outright — the vast
    // majority of common camera resolutions (640x480, 1280x720, 1920x1080) already are.
    private const int RequiredDimensionMultiple = 16;

    // Video RTP typically runs a fixed 90kHz clock regardless of actual capture frame rate.
    // Frames arrive whenever the camera pipeline delivers them (no timer of our own pacing
    // capture), so this assumes a nominal 30fps for the RTP timestamp increment per frame — an
    // approximation reasonable for a first working slice; revisit with real elapsed-time-based
    // timestamps if live testing shows playback pacing issues, the same way audio's pacing bugs
    // were only found and fixed via real testing, not guessed up front.
    private const uint VideoClockRateHz = 90000;
    private const uint AssumedFrameRate = 30;
    private const uint AssumedDurationRtpUnits = VideoClockRateHz / AssumedFrameRate;

    // Not static readonly — confirmed live in AndroidAudioEndPoint/CustomWindowsAudioEndPoint
    // that a static readonly logger field can permanently capture SIPSorcery's no-op default
    // logger if this class is constructed (as it is, in MauiProgram.cs) before
    // SIPSorcery.LogFactory.Set() wires up the real ILoggerFactory.
    private static ILogger Logger => SIPSorcery.LogFactory.CreateLogger<CustomWindowsVideoEndPoint>();

    // Separate encoder/decoder instances — NOT one shared VP8Codec. Same fix as the Android
    // counterpart (AndroidCameraXVideoEndPoint, issue #193/PR #194): a single VP8Codec called from
    // two different threads at once (EncodeVideo from AnalyzeAsync, on whatever thread Shiny's
    // CameraView analyzer dispatches to; DecodeVideo from GotVideoFrame, on whatever thread
    // SIPSorcery dispatches incoming RTP video to) with no synchronization risks the same native
    // memory-corruption crash confirmed on Android — see #196. Two independent instances removes
    // the shared state entirely instead of adding locking around one.
    private readonly VP8Codec videoEncoder = new();
    private readonly VP8Codec videoDecoder = new();
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

    public event EncodedSampleDelegate? OnVideoSourceEncodedSample;
    public event RawVideoSampleDelegate? OnVideoSourceRawSample;
    public event RawVideoSampleFasterDelegate? OnVideoSourceRawSampleFaster;
    public event SourceErrorDelegate? OnVideoSourceError;
    public event VideoSinkSampleDecodedDelegate? OnVideoSinkDecodedSample;
    public event VideoSinkSampleDecodedFasterDelegate? OnVideoSinkDecodedSampleFaster;

    // Raw decoded remote frames (BGR, per VP8Codec.DecodeVideo's internal conversion), for a UI
    // layer to render — this class deliberately doesn't own any rendering surface itself.
    public event Action<int, int, byte[]>? DecodedFrameReady;

    public CustomWindowsVideoEndPoint()
    {
        this.sourceFormatManager = new MediaFormatManager<VideoFormat>(this.videoEncoder.SupportedFormats);
        this.sinkFormatManager = new MediaFormatManager<VideoFormat>(this.videoDecoder.SupportedFormats);
    }

    // Called by the call page once its CameraView is available (e.g. OnAppearing) — wires this
    // endpoint up as the view's frame analyzer so captured frames flow into AnalyzeAsync below.
    public void AttachCameraView(CameraView cameraView)
    {
        this.attachedCameraView = cameraView;
        cameraView.Analyzer = this;
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

    public string Id => nameof(CustomWindowsVideoEndPoint);

    public bool WantsFrame() => !this.isSourcePaused && this.HasEncodedVideoSubscribers();

    public ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not WindowsCameraFrame windowsFrame)
        {
            return ValueTask.FromResult<IReadOnlyList<OverlayBox>?>(null);
        }

        if (this.isSourcePaused || this.sourceFormatManager.SelectedFormat.IsEmpty())
        {
            return ValueTask.FromResult<IReadOnlyList<OverlayBox>?>(null);
        }

        if (windowsFrame.Width % RequiredDimensionMultiple != 0 || windowsFrame.Height % RequiredDimensionMultiple != 0)
        {
            this.framesSkippedSinceLog++;
            this.LogEncodeSummaryIfDue();
            return ValueTask.FromResult<IReadOnlyList<OverlayBox>?>(null);
        }

        try
        {
            byte[] encodedSample = this.videoEncoder.EncodeVideo(
                windowsFrame.Width,
                windowsFrame.Height,
                windowsFrame.Bgra,
                VideoPixelFormatsEnum.Bgra,
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

    public void ForceKeyFrame() => this.videoEncoder.ForceKeyFrame();

    public bool HasEncodedVideoSubscribers() => this.OnVideoSourceEncodedSample is not null;

    public bool IsVideoSourcePaused() => this.isSourcePaused;

    public Task StartVideo()
    {
        this.isSourcePaused = false;
        return this.attachedCameraView?.StartAsync() ?? Task.CompletedTask;
    }

    public Task CloseVideo()
    {
        return this.attachedCameraView?.StopAsync() ?? Task.CompletedTask;
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
            foreach (VideoSample sample in this.videoDecoder.DecodeVideo(payload, VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.VP8))
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
        this.videoEncoder.Dispose();
        this.videoDecoder.Dispose();
    }
}
