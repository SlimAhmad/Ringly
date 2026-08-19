using System.Net;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;
using Vpx.Net;

namespace Ringly.Samples.Maui.Platforms.Android;

// Hand-rolled CameraX capture, bypassing Shiny.Maui.Controls.Camera's IFrameAnalyzer pipeline
// entirely — built specifically to A/B test against AndroidVideoEndPoint.cs after live testing
// showed Shiny's own analyzer callback (WantsFrame/AnalyzeAsync) never firing despite every
// documented precondition being satisfied (see issue #143's full trace into Shiny's own source:
// FrameAnalyzerBridge -> CameraPipeline.WantsFrame -> our IFrameAnalyzer, blocked by private
// state inside the library invisible to any logging addable from our own files). This class
// drives CameraX's ProcessCameraProvider and ImageAnalysis use case DIRECTLY — no Shiny
// CameraView, no Shiny CameraPipeline in between — so if this implementation successfully
// delivers frames where the Shiny-based one didn't, that confirms the gap is inside Shiny's
// library rather than a mistake in how we drove it.
//
// Deliberately minimal: no local self-preview surface (no Preview use case bound) — CameraX
// doesn't require Preview to be part of the bound use-case set, and this first pass is purely
// about confirming the capture -> encode -> RTP -> decode pipeline itself works, the same phased
// approach used throughout this session's video work. AttachCameraView/DetachCameraView are
// therefore no-ops here (see IAndroidVideoCaptureEndPoint) so CallPage's existing wiring compiles
// and runs unchanged regardless of which implementation MauiProgram.cs registers. No rotation
// handling either (Shiny's own BindUseCases computes a target rotation we have no equivalent
// source for here) — video may appear sideways depending on device orientation; acceptable for a
// first diagnostic pass, revisit if this implementation proves out and becomes the real one.
public sealed class AndroidCameraXVideoEndPoint : IVideoSource, IVideoSink, IAndroidVideoCaptureEndPoint, IDisposable
{
    private const int RequiredDimensionMultiple = 16;

    private const uint VideoClockRateHz = 90000;

    // Both the throttle interval below and the RTP timestamp increment per sent frame are derived
    // from this one value — confirmed live that leaving AssumedDurationRtpUnits at its old
    // fixed-30fps value while actually encoding (and sending) at a throttled lower rate would
    // have caused the RTP timestamp to under-advance relative to real elapsed time, misleading the
    // receiver's own pacing/jitter-buffer logic. See OnFrameAnalyzed's own comment for why the
    // throttle exists at all (encoding every camera-delivered frame was starving audio's real-time
    // threads on a real device).
    private const uint TargetEncodeFrameRate = 15;
    private const uint AssumedDurationRtpUnits = VideoClockRateHz / TargetEncodeFrameRate;
    private static readonly TimeSpan MinEncodeInterval = TimeSpan.FromSeconds(1.0 / TargetEncodeFrameRate);

    // Not static readonly — confirmed live (see AndroidAudioEndPoint/CustomWindowsAudioEndPoint)
    // that a static readonly logger field can permanently capture SIPSorcery's no-op default
    // logger if this class is constructed before SIPSorcery.LogFactory.Set() wires up the real
    // ILoggerFactory.
    private static ILogger Logger => SIPSorcery.LogFactory.CreateLogger<AndroidCameraXVideoEndPoint>();

    private readonly VP8Codec videoCodec = new();
    private readonly MediaFormatManager<VideoFormat> sourceFormatManager;
    private readonly MediaFormatManager<VideoFormat> sinkFormatManager;
    private readonly CameraLifecycleOwner lifecycleOwner = new();

    private ProcessCameraProvider? cameraProvider;
    private ImageAnalysis? imageAnalysis;
    private Java.Util.Concurrent.IExecutorService? analysisExecutor;
    private bool isSourcePaused;
    private bool isSinkPaused;
    private bool hasLoggedFirstAnalyzeCall;
    private bool isUsingFrontCamera;
    private DateTime lastEncodeAt = DateTime.MinValue;

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

    public event Action<int, int, byte[]>? DecodedFrameReady;

    public AndroidCameraXVideoEndPoint()
    {
        this.sourceFormatManager = new MediaFormatManager<VideoFormat>(this.videoCodec.SupportedFormats);
        this.sinkFormatManager = new MediaFormatManager<VideoFormat>(this.videoCodec.SupportedFormats);
    }

    // No Shiny CameraView involved in this implementation — see the class comment for why these
    // are no-ops rather than throwing, so CallPage's shared wiring works unchanged either way.
    public void AttachCameraView(Shiny.Maui.Controls.Camera.CameraView cameraView)
    {
    }

    public void DetachCameraView()
    {
    }

    // Toggles front/back and rebinds while the camera is already running (BindCameraAsync's own
    // "cameraProvider.UnbindAll()" call handles switching cleanly — CameraX requires the previous
    // binding torn down before a different CameraSelector can bind). A no-op if the camera hasn't
    // started yet (StartVideo will pick up isUsingFrontCamera's current value when it does).
    public Task SwitchCameraAsync()
    {
        this.isUsingFrontCamera = !this.isUsingFrontCamera;

        return this.cameraProvider is null
            ? Task.CompletedTask
            : MainThread.InvokeOnMainThreadAsync(this.BindCameraAsync);
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
        return MainThread.InvokeOnMainThreadAsync(this.BindCameraAsync);
    }

    public Task CloseVideo()
    {
        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            this.cameraProvider?.UnbindAll();
            this.lifecycleOwner.Stop();
        });
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

    // Binds ProcessCameraProvider + a single ImageAnalysis use case directly against CameraX, with
    // no Shiny.Maui.Controls.Camera involvement at all. Same GetInstance/AddListener idiom Shiny's
    // own CameraViewHandler.Android.cs uses internally (confirmed via its real source) — just
    // driven by our own code instead of through their CameraView/CameraPipeline abstraction.
    private Task BindCameraAsync()
    {
        var bindCompletionSource = new TaskCompletionSource();
        var context = global::Android.App.Application.Context;
        var future = ProcessCameraProvider.GetInstance(context);

        future.AddListener(new Java.Lang.Runnable(() =>
        {
            try
            {
                this.cameraProvider = (ProcessCameraProvider)future.Get()!;
                this.lifecycleOwner.Start();

                this.analysisExecutor ??= Java.Util.Concurrent.Executors.NewSingleThreadExecutor();

                // Deliberately NOT calling SetTargetResolution here (tried it, reverted — see
                // issue #187): it's a deprecated best-effort hint, and CameraX resolves it against
                // whatever discrete stream configurations the device's camera HAL actually
                // advertises, picking the "closest" one by its own aspect-ratio/area heuristic —
                // NOT necessarily anything close to or smaller than the value passed in. Confirmed
                // live on this device: requesting 320x240 got CameraX to bind 1088x1088 instead
                // (~3.9x MORE pixels than the unset default of 640x480, logged via "Native CameraX
                // Analyze() called for the first time"), which made the already-slow software VP8
                // encode dramatically worse — encode rate dropped further, audio concealment got
                // worse, and the app crashed under the load. Left at CameraX's own default (640x480
                // on this device) until a real fix (e.g. downscaling the captured frame ourselves
                // in ConvertYuv420888ToI420, or CameraX's newer ResolutionSelector API with an
                // explicit allowed-resolutions list) is implemented and verified live.
                this.imageAnalysis = new ImageAnalysis.Builder()
                    .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest!)!
                    .Build();
                this.imageAnalysis!.SetAnalyzer(this.analysisExecutor!, new NativeFrameAnalyzer(this));

                var selector = new CameraSelector.Builder()
                    .RequireLensFacing(this.isUsingFrontCamera ? CameraSelector.LensFacingFront : CameraSelector.LensFacingBack)!
                    .Build();

                this.cameraProvider.UnbindAll();
                this.cameraProvider.BindToLifecycle(this.lifecycleOwner, selector, this.imageAnalysis);

                Logger.LogInformation(
                    "CameraX bound directly (no Shiny) — ImageAnalysis use case active, facing {Facing}.",
                    this.isUsingFrontCamera ? "front" : "back");
                bindCompletionSource.TrySetResult();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to bind CameraX directly.");
                this.OnVideoSourceError?.Invoke($"CameraX bind failed: {exception.Message}");
                bindCompletionSource.TrySetException(exception);
            }
        }), ContextCompat.GetMainExecutor(context));

        return bindCompletionSource.Task;
    }

    // CameraX's native ImageAnalysis.IAnalyzer — called directly by CameraX on the analysis
    // executor thread, with no Shiny CameraPipeline/back-pressure layer in between. Confirms
    // whether frames genuinely reach an analyzer driven this way, answering #143's open question.
    private sealed class NativeFrameAnalyzer(AndroidCameraXVideoEndPoint owner) : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        public void Analyze(IImageProxy image) => owner.OnFrameAnalyzed(image);

        // ImageAnalysis.Analyzer's other members have default implementations on the Java side,
        // but confirmed live that CameraX's JNI dispatch still throws AbstractMethodError calling
        // them if this binding doesn't implement them explicitly — every single BindToLifecycle
        // call failed with exactly that ("abstract method ... getDefaultTargetResolution() on
        // receiver ...NativeFrameAnalyzer"), which was silently retried on every reconnection
        // attempt and is the real explanation for the choppiness reported while this bug was live.
        public global::Android.Util.Size? DefaultTargetResolution => null;

        public int TargetCoordinateSystem => 0; // COORDINATE_SYSTEM_ORIGINAL — matches the Java default

        public void UpdateTransform(global::Android.Graphics.Matrix? matrix)
        {
        }
    }

    private void OnFrameAnalyzed(IImageProxy image)
    {
        try
        {
            if (!this.hasLoggedFirstAnalyzeCall)
            {
                this.hasLoggedFirstAnalyzeCall = true;
                Logger.LogInformation(
                    "Native CameraX Analyze() called for the first time — {Width}x{Height}.",
                    image.Width,
                    image.Height);
            }

            if (this.isSourcePaused || this.sourceFormatManager.SelectedFormat.IsEmpty())
            {
                return;
            }

            int width = image.Width;
            int height = image.Height;

            if (width % RequiredDimensionMultiple != 0 || height % RequiredDimensionMultiple != 0)
            {
                this.framesSkippedSinceLog++;
                this.LogEncodeSummaryIfDue();
                return;
            }

            // Throttle to TargetEncodeFrameRate instead of encoding every single frame CameraX
            // delivers (its analysis executor typically hands frames at the sensor's native rate,
            // ~30fps). Confirmed live via a real device: encoding every frame with
            // SIPSorcery.VP8 — a pure managed/software codec, no hardware acceleration — was
            // saturating enough CPU to starve AndroidAudioEndPoint's own real-time capture/
            // playback threads, producing sustained ~20-35% audio frame loss/concealment for the
            // whole duration of any video call, not just occasional jitter. Throttling the
            // encoder's own workload (not the camera's capture rate, which CameraX still delivers
            // at full speed to WantsFrame/Analyze — this just skips MOST of those deliveries
            // before the expensive part) trades outgoing video smoothness for audio staying
            // intact, which is the right tradeoff for a voice-first calling app.
            if (this.lastEncodeAt != DateTime.MinValue && DateTime.UtcNow - this.lastEncodeAt < MinEncodeInterval)
            {
                return;
            }

            this.lastEncodeAt = DateTime.UtcNow;

            byte[] i420Sample = ConvertYuv420888ToI420(image, width, height);

            byte[] encodedSample = this.videoCodec.EncodeVideo(
                width,
                height,
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
        finally
        {
            // CameraX's contract: the analyzer must close the image or no further frames are
            // delivered — with StrategyKeepOnlyLatest's single-slot queue, failing to close here
            // would stall the pipeline after exactly one frame.
            image.Close();
        }
    }

    // Same YUV_420_888 -> I420 repacking as AndroidVideoEndPoint.cs — see that file's comment for
    // why each plane's own RowStride/PixelStride must be walked explicitly rather than assuming a
    // fixed layout. Duplicated rather than shared, matching this repo's existing convention of
    // keeping each platform/implementation endpoint self-contained (CustomWindowsAudioEndPoint and
    // AndroidAudioEndPoint don't share code either despite similar shape).
    private static byte[] ConvertYuv420888ToI420(IImageProxy proxy, int width, int height)
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

    private static void CopyPlane(IImageProxyPlaneProxy plane, int planeWidth, int planeHeight, byte[] destination, int destinationOffset)
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

    // IVideoSink — identical to AndroidVideoEndPoint's.

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
        this.cameraProvider?.UnbindAll();
        this.lifecycleOwner.Destroy();
        this.videoCodec.Dispose();
    }
}

// Minimal standalone ILifecycleOwner CameraX drives directly — same shape as
// Shiny.Maui.Controls.Camera's own internal CameraLifecycleOwner (confirmed via its real source),
// reimplemented here since that type is internal to Shiny's own assembly and not visible to ours.
internal sealed class CameraLifecycleOwner : Java.Lang.Object, ILifecycleOwner
{
    private readonly LifecycleRegistry registry;

    public CameraLifecycleOwner()
    {
        this.registry = new LifecycleRegistry(this);
        this.registry.SetCurrentState(Lifecycle.State.Initialized!);
    }

    public Lifecycle Lifecycle => this.registry;

    public void Start() => this.registry.SetCurrentState(Lifecycle.State.Resumed!);

    public void Stop() => this.registry.SetCurrentState(Lifecycle.State.Created!);

    public void Destroy() => this.registry.SetCurrentState(Lifecycle.State.Destroyed!);
}
