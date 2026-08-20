using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using NAudio.Wave;
using SIPSorceryMedia.Abstractions;

namespace Ringly.Samples.Maui.Platforms.Windows;

// SIPSorceryMedia.Windows's WindowsAudioEndPoint, and this class's own first attempt using
// NAudio's legacy WaveInEvent (the old WinMM capture API), both reported every step succeeding
// (device initializes, StartRecording() doesn't throw, callbacks fire on schedule) yet produced
// a perfectly constant near-zero PCM amplitude for over a minute straight — not fluctuating even
// slightly, which real background noise always does, meaning something below the application was
// feeding silence into the buffer rather than the mic genuinely being quiet. Confirmed working
// apps on this same machine using this same physical microphone (Teams, Zoom, Discord) all use
// the modern WASAPI capture path, not legacy WinMM — Windows' privacy/audio-session behavior is
// known to silently zero out WinMM captures for unsigned third-party apps in some configurations
// while still reporting "Allow" in Settings (which governs the modern WASAPI permission path).
// This switches capture to WasapiCapture on the Communications-role default device (the same
// role real VoIP apps target), with a resampler down to the 8kHz mono this pipeline needs since
// WASAPI shared-mode only offers the device's own native mix format (typically 44.1/48kHz).
public sealed class CustomWindowsAudioEndPoint : IAudioSource, IAudioSink, IDisposable
{
    private const int SampleRate = 8000;
    private const int BitsPerSample = 16;
    private const int Channels = 1;

    // Not static readonly — confirmed live in AndroidAudioEndPoint that a static readonly logger
    // field can permanently capture SIPSorcery's no-op default logger if this class is
    // constructed (as it is, in MauiProgram.cs) before SIPSorcery.LogFactory.Set() wires up the
    // real ILoggerFactory, silently discarding every log call for the app's entire lifetime.
    private static ILogger Logger => SIPSorcery.LogFactory.CreateLogger<CustomWindowsAudioEndPoint>();

    private readonly IAudioEncoder audioEncoder;
    private readonly MediaFormatManager<AudioFormat> sourceFormatManager;
    private readonly MediaFormatManager<AudioFormat> sinkFormatManager;
    private readonly object sinkLock = new();

    private WasapiCapture? waveIn;
    private MediaFoundationResampler? captureResampler;
    private BufferedWaveProvider? captureNativeBuffer;
    private Thread? captureResampleThread;
    private volatile bool isCapturing;
    private MMDevice? captureDeviceForDiagnostics;
    private Thread? deviceMeterDiagnosticsThread;
    private IWavePlayer? waveOut;
    private BufferedWaveProvider? waveProvider;
    private bool isSourcePaused;
    private bool isSinkPaused;

    private int framesCapturedSinceLog;
    private short peakCaptureAmplitudeSinceLog;
    private DateTime lastCaptureLogAt = DateTime.MinValue;

    private float peakRawNativeAmplitudeSinceLog;
    private int rawNativeCallbacksSinceLog;
    private DateTime lastRawNativeLogAt = DateTime.MinValue;

    private int framesWrittenSincePlaybackLog;
    private short peakPlaybackAmplitudeSinceLog;
    private DateTime lastPlaybackLogAt = DateTime.MinValue;
    private DateTime lastPlaybackSkipLogAt = DateTime.MinValue;

    public event EncodedSampleDelegate? OnAudioSourceEncodedSample;
    public event Action<EncodedAudioFrame>? OnAudioSourceEncodedFrameReady;
    public event RawAudioSampleDelegate? OnAudioSourceRawSample;
    public event SourceErrorDelegate? OnAudioSourceError;
    public event SourceErrorDelegate? OnAudioSinkError;

    public CustomWindowsAudioEndPoint(IAudioEncoder audioEncoder)
    {
        this.audioEncoder = audioEncoder;
        this.sourceFormatManager = new MediaFormatManager<AudioFormat>(audioEncoder.SupportedFormats);
        this.sinkFormatManager = new MediaFormatManager<AudioFormat>(audioEncoder.SupportedFormats);

        // Required once before any MediaFoundationResampler use — a no-op if already started
        // elsewhere in the process, but this is the only place in the app that needs it.
        try
        {
            MediaFoundationApi.Startup();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "MediaFoundationApi.Startup() failed.");
        }

        this.InitPlaybackDevice();
    }

    public List<AudioFormat> GetAudioSourceFormats() => this.sourceFormatManager.GetSourceFormats();

    public List<AudioFormat> GetAudioSinkFormats() => this.sinkFormatManager.GetSourceFormats();

    public void SetAudioSourceFormat(AudioFormat audioFormat)
    {
        Logger.LogInformation("SetAudioSourceFormat called with {FormatName} ({ClockRate}Hz).", audioFormat.FormatName, audioFormat.ClockRate);
        this.sourceFormatManager.SetSelectedFormat(audioFormat);
    }

    public void SetAudioSinkFormat(AudioFormat audioFormat)
    {
        Logger.LogInformation("SetAudioSinkFormat called with {FormatName} ({ClockRate}Hz).", audioFormat.FormatName, audioFormat.ClockRate);
        this.sinkFormatManager.SetSelectedFormat(audioFormat);
    }

    public void RestrictFormats(Func<AudioFormat, bool> filter)
    {
        this.sourceFormatManager.RestrictFormats(filter);
        this.sinkFormatManager.RestrictFormats(filter);
    }

    public bool HasEncodedAudioSubscribers() => this.OnAudioSourceEncodedSample is not null;

    public bool IsAudioSourcePaused() => this.isSourcePaused;

    public bool IsAudioSinkPaused() => this.isSinkPaused;

    public void ExternalAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds, short[] sample) =>
        throw new NotImplementedException();

    public Task StartAudio()
    {
        if (this.waveIn is not null)
        {
            return Task.CompletedTask;
        }

        try
        {
            var deviceEnumerator = new MMDeviceEnumerator();

            // Communications role — the same role real VoIP apps (Teams, Zoom, Discord) target,
            // as opposed to Console/Multimedia which govern general system sound.
            MMDevice captureDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);

            // Native WASAPI ground truth, read/written directly via IAudioEndpointVolume (NAudio's
            // CoreAudioApi is a thin wrapper over this exact COM interface — no capture session
            // involved). Confirmed via live testing that this endpoint's own mute flag — distinct
            // from the per-app Volume Mixer entry, which stays independently mutable — was the
            // actual cause of captured audio being genuine, exact-zero silence: the device volume
            // level was fine (0.883) but Mute was true, which WASAPI enforces by zeroing the stream
            // before it ever reaches our capture callback. Force it unmuted here so a call doesn't
            // silently fail again the same way if something (a driver utility, a hotkey, another
            // app) re-mutes the endpoint between runs.
            try
            {
                bool wasMuted = captureDevice.AudioEndpointVolume.Mute;
                Logger.LogInformation(
                    "Native WASAPI endpoint state for \"{DeviceName}\": Mute={Mute}, MasterVolumeLevelScalar={Volume:F3}.",
                    captureDevice.FriendlyName,
                    wasMuted,
                    captureDevice.AudioEndpointVolume.MasterVolumeLevelScalar);

                if (wasMuted)
                {
                    captureDevice.AudioEndpointVolume.Mute = false;
                    Logger.LogWarning(
                        "Capture device \"{DeviceName}\" was muted at the OS/driver level — forced it unmuted so this call can capture audio.",
                        captureDevice.FriendlyName);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to read/clear native WASAPI endpoint mute state.");
            }

            this.captureDeviceForDiagnostics = captureDevice;

            this.waveIn = new WasapiCapture(captureDevice);

            // WASAPI shared mode only offers the device's own native mix format (typically
            // 44.1kHz or 48kHz, often stereo float) — this pipeline needs 8kHz mono 16-bit PCM
            // for the telephony codecs in use, so captured audio is buffered at its native format
            // and pulled through a resampler on a dedicated thread (mirrors
            // AndroidAudioEndPoint's capture loop) rather than converted inline in the WASAPI
            // callback.
            var nativeBuffer = new BufferedWaveProvider(this.waveIn.WaveFormat) { DiscardOnBufferOverflow = true };
            this.captureNativeBuffer = nativeBuffer;
            this.captureResampler = new MediaFoundationResampler(nativeBuffer, new WaveFormat(SampleRate, BitsPerSample, Channels));

            WaveFormat nativeFormat = this.waveIn.WaveFormat;

            this.waveIn.DataAvailable += (_, e) =>
            {
                nativeBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
                this.LogRawNativeAmplitude(e.Buffer, e.BytesRecorded, nativeFormat);
            };

            this.waveIn.RecordingStopped += (_, e) =>
            {
                if (e.Exception is not null)
                {
                    Logger.LogError(e.Exception, "WasapiCapture recording stopped due to an exception.");
                    this.OnAudioSourceError?.Invoke($"Recording stopped: {e.Exception.Message}");
                }
            };

            this.waveIn.StartRecording();

            Logger.LogInformation(
                "WasapiCapture.StartRecording() called on Communications-role device \"{DeviceName}\", native format {NativeFormat}.",
                captureDevice.FriendlyName,
                this.waveIn.WaveFormat);

            this.isCapturing = true;
            this.captureResampleThread = new Thread(this.CaptureResampleLoop) { IsBackground = true, Name = "WindowsAudioCaptureResample" };
            this.captureResampleThread.Start();
            this.deviceMeterDiagnosticsThread = new Thread(this.DeviceMeterDiagnosticsLoop) { IsBackground = true, Name = "WindowsAudioDeviceMeterDiagnostics" };
            this.deviceMeterDiagnosticsThread.Start();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "WASAPI capture setup failed.");
            this.OnAudioSourceError?.Invoke($"WASAPI capture setup failed: {exception.Message}");
        }

        return Task.CompletedTask;
    }

    // Diagnostic only: reads the RAW bytes WASAPI's DataAvailable callback hands us, before
    // AddSamples/the resampler touch them at all — isolates whether WASAPI itself is capturing
    // real audio (this would be non-zero) versus the resampling stage silently producing zeros
    // from genuinely-captured input (this would stay zero while the post-resample log is also
    // zero, which is exactly what's been observed so far).
    private void LogRawNativeAmplitude(byte[] buffer, int bytesRecorded, WaveFormat nativeFormat)
    {
        float peak = 0f;

        if (nativeFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            for (int i = 0; i + 4 <= bytesRecorded; i += 4)
            {
                float sample = Math.Abs(BitConverter.ToSingle(buffer, i));
                if (sample > peak)
                {
                    peak = sample;
                }
            }
        }
        else
        {
            for (int i = 0; i + 2 <= bytesRecorded; i += 2)
            {
                float sample = Math.Abs((float)BitConverter.ToInt16(buffer, i) / short.MaxValue);
                if (sample > peak)
                {
                    peak = sample;
                }
            }
        }

        if (peak > this.peakRawNativeAmplitudeSinceLog)
        {
            this.peakRawNativeAmplitudeSinceLog = peak;
        }

        this.rawNativeCallbacksSinceLog++;

        if (DateTime.UtcNow - this.lastRawNativeLogAt > TimeSpan.FromSeconds(2))
        {
            Logger.LogInformation(
                "RAW WASAPI native buffer: {Callbacks} callbacks in the last ~2s, peak normalized amplitude {PeakAmplitude:F6} (of 1.0 max, encoding {Encoding}) — BEFORE any resampling.",
                this.rawNativeCallbacksSinceLog,
                this.peakRawNativeAmplitudeSinceLog,
                nativeFormat.Encoding);
            this.lastRawNativeLogAt = DateTime.UtcNow;
            this.rawNativeCallbacksSinceLog = 0;
            this.peakRawNativeAmplitudeSinceLog = 0;
        }
    }

    // Polls the native WASAPI peak meter directly (IAudioMeterInformation via NAudio's
    // AudioMeterInformation wrapper) — this reflects what the OS's audio engine measures at the
    // endpoint itself, upstream of any capture session, format negotiation, or app-level state.
    // Non-zero here while our own capture pipeline still reads zero would prove the problem is
    // between the engine and our capture session (e.g. stream category / exclusive-mode contention);
    // zero here proves it further upstream (mute, driver, or hardware), before Windows' own mixer
    // even sees a signal.
    private void DeviceMeterDiagnosticsLoop()
    {
        MMDevice? device = this.captureDeviceForDiagnostics;
        if (device is null)
        {
            return;
        }

        float peakSinceLog = 0f;
        DateTime lastLogAt = DateTime.MinValue;

        while (this.isCapturing)
        {
            try
            {
                float peak = device.AudioMeterInformation.MasterPeakValue;
                if (peak > peakSinceLog)
                {
                    peakSinceLog = peak;
                }

                if (device.AudioEndpointVolume.Mute)
                {
                    Logger.LogWarning(
                        "Capture device \"{DeviceName}\" was re-muted mid-call — forcing it unmuted again.",
                        device.FriendlyName);
                    device.AudioEndpointVolume.Mute = false;
                }

                if (DateTime.UtcNow - lastLogAt > TimeSpan.FromSeconds(2))
                {
                    Logger.LogInformation(
                        "Native WASAPI peak meter for \"{DeviceName}\": peak {PeakValue:F6} (of 1.0 max) over the last ~2s — measured by the OS engine, independent of our capture session. Mute={Mute}, Volume={Volume:F3}.",
                        device.FriendlyName,
                        peakSinceLog,
                        device.AudioEndpointVolume.Mute,
                        device.AudioEndpointVolume.MasterVolumeLevelScalar);
                    lastLogAt = DateTime.UtcNow;
                    peakSinceLog = 0f;
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to read native WASAPI peak meter.");
                return;
            }

            Thread.Sleep(200);
        }
    }

    private void CaptureResampleLoop()
    {
        const int FrameDurationMs = 20;
        int frameBytes = SampleRate * BitsPerSample / 8 * FrameDurationMs / 1000;
        var buffer = new byte[frameBytes];

        // Confirmed live that gating purely on "is enough native audio buffered yet" — with no
        // pacing of the READS themselves — lets this loop drain any surplus sitting in the
        // native buffer back-to-back with zero delay between reads, the instant the gate is
        // satisfied. Requiring a large threshold (the previous, dimensionally-wrong 120ms) masked
        // this by accident, since accumulating 120ms of surplus itself takes ~120ms of wall
        // time — but fixing that threshold to the dimensionally-correct 20ms removed that
        // accidental throttling and exposed the real bug: capturing/encoding/sending audio at
        // ~17x real-time speed (1700-1800 "20ms frames" logged per 2 real seconds instead of the
        // correct ~100), which is what turned into the reported "static" — packets arriving at
        // the receiver far faster than real-time, well beyond what the jitter buffer's ~200ms cap
        // can absorb. A steady clocked cadence (mirroring PlaybackFeedLoop's approach on the
        // Android side) is the actual fix: read at most one frame per real 20ms tick, regardless
        // of how much surplus is sitting in the buffer.
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        long nextFrameDueAtMs = 0;

        while (this.isCapturing && this.captureResampler is not null && this.captureNativeBuffer is not null)
        {
            if (this.captureNativeBuffer.BufferedDuration.TotalMilliseconds < FrameDurationMs)
            {
                Thread.Sleep(5);
                continue;
            }

            int bytesRead = this.captureResampler.Read(buffer, 0, buffer.Length);

            nextFrameDueAtMs += FrameDurationMs;
            int sleepMs = (int)(nextFrameDueAtMs - stopwatch.ElapsedMilliseconds);

            if (sleepMs > 0)
            {
                Thread.Sleep(sleepMs);
            }
            else
            {
                // Fell behind real time — resync instead of racing to catch up, which is exactly
                // the burst behavior this pacing exists to prevent.
                nextFrameDueAtMs = stopwatch.ElapsedMilliseconds;
            }

            if (bytesRead <= 0 || this.isSourcePaused || this.sourceFormatManager.SelectedFormat.IsEmpty())
            {
                continue;
            }

            int sampleCount = bytesRead / 2;
            var pcm = new short[sampleCount];
            Buffer.BlockCopy(buffer, 0, pcm, 0, bytesRead);

            for (int i = 0; i < pcm.Length; i++)
            {
                short absSample = pcm[i] < 0 ? (short)-pcm[i] : pcm[i];
                if (absSample > this.peakCaptureAmplitudeSinceLog)
                {
                    this.peakCaptureAmplitudeSinceLog = absSample;
                }
            }

            byte[] encodedSample = this.audioEncoder.EncodeAudio(pcm, this.sourceFormatManager.SelectedFormat);
            this.OnAudioSourceEncodedSample?.Invoke((uint)encodedSample.Length, encodedSample);

            this.framesCapturedSinceLog++;

            if (DateTime.UtcNow - this.lastCaptureLogAt > TimeSpan.FromSeconds(2))
            {
                Logger.LogInformation(
                    "Captured {FramesCaptured} frames in the last ~2s, peak PCM amplitude {PeakAmplitude} (of 32767 max — near 0 means the mic is capturing silence).",
                    this.framesCapturedSinceLog,
                    this.peakCaptureAmplitudeSinceLog);
                this.lastCaptureLogAt = DateTime.UtcNow;
                this.framesCapturedSinceLog = 0;
                this.peakCaptureAmplitudeSinceLog = 0;
            }
        }
    }

    public Task CloseAudio()
    {
        this.isCapturing = false;
        this.captureResampleThread?.Join(TimeSpan.FromMilliseconds(500));
        this.captureResampleThread = null;
        this.deviceMeterDiagnosticsThread?.Join(TimeSpan.FromMilliseconds(500));
        this.deviceMeterDiagnosticsThread = null;
        this.captureDeviceForDiagnostics = null;

        if (this.waveIn is not null)
        {
            this.waveIn.StopRecording();
            this.waveIn.Dispose();
            this.waveIn = null;
        }

        this.captureResampler?.Dispose();
        this.captureResampler = null;
        this.captureNativeBuffer = null;

        return Task.CompletedTask;
    }

    public Task PauseAudio()
    {
        this.isSourcePaused = true;
        return Task.CompletedTask;
    }

    public Task ResumeAudio()
    {
        this.isSourcePaused = false;
        return Task.CompletedTask;
    }

    private void InitPlaybackDevice()
    {
        this.waveProvider = new BufferedWaveProvider(new WaveFormat(SampleRate, BitsPerSample, Channels))
        {
            DiscardOnBufferOverflow = true
        };

        // Same Communications-role targeting as StartAudio's capture device, for the same reason
        // (see its own comment) — confirmed live that plain WaveOutEvent() plays through WinMM's
        // default device (-1, "default"), which follows the OS's general Multimedia-role output
        // and can silently differ from the Communications-role device real VoIP apps (and this
        // class's own capture side) actually use. Confirmed as a genuine live bug, not a guess:
        // decoded remote audio was reaching WaveOutEvent.Play()="Playing" with real non-zero PCM
        // amplitude (WritePlayback's own diagnostics proved this) while still being completely
        // inaudible — exactly the symptom of audio correctly reaching the wrong output device.
        try
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            MMDevice playbackDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications);

            Logger.LogInformation(
                "Using Communications-role playback device \"{DeviceName}\" for call audio output.",
                playbackDevice.FriendlyName);

            this.waveOut = new WasapiOut(playbackDevice, AudioClientShareMode.Shared, true, 50);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to select Communications-role playback device — falling back to the default output device.");
            this.waveOut = new WaveOutEvent();
        }

        this.waveOut.Init(this.waveProvider);
    }

    [Obsolete("Use GotEncodedMediaFrame instead.")]
    public void GotAudioRtp(System.Net.IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload) =>
        this.WritePlayback(payload, this.sinkFormatManager.SelectedFormat);

    public void GotEncodedMediaFrame(EncodedAudioFrame encodedMediaFrame)
    {
        if (!encodedMediaFrame.AudioFormat.IsEmpty())
        {
            this.WritePlayback(encodedMediaFrame.EncodedAudio, encodedMediaFrame.AudioFormat);
        }
    }

    private void WritePlayback(byte[] encodedAudio, AudioFormat format)
    {
        if (this.isSinkPaused || format.IsEmpty())
        {
            if (DateTime.UtcNow - this.lastPlaybackSkipLogAt > TimeSpan.FromSeconds(2))
            {
                this.lastPlaybackSkipLogAt = DateTime.UtcNow;
                Logger.LogWarning(
                    "Playback packet skipped: isSinkPaused={IsSinkPaused}, formatIsEmpty={FormatIsEmpty}.",
                    this.isSinkPaused,
                    format.IsEmpty());
            }

            return;
        }

        short[] pcm = this.audioEncoder.DecodeAudio(encodedAudio, format);
        byte[] pcmBytes = new byte[pcm.Length * 2];
        Buffer.BlockCopy(pcm, 0, pcmBytes, 0, pcmBytes.Length);

        for (int i = 0; i < pcm.Length; i++)
        {
            short absSample = pcm[i] < 0 ? (short)-pcm[i] : pcm[i];
            if (absSample > this.peakPlaybackAmplitudeSinceLog)
            {
                this.peakPlaybackAmplitudeSinceLog = absSample;
            }
        }

        lock (this.sinkLock)
        {
            this.waveProvider?.AddSamples(pcmBytes, 0, pcmBytes.Length);
        }

        this.framesWrittenSincePlaybackLog++;

        if (DateTime.UtcNow - this.lastPlaybackLogAt > TimeSpan.FromSeconds(2))
        {
            Logger.LogInformation(
                "Wrote {FramesWritten} playback frames in the last ~2s (PlaybackState={PlaybackState}), peak PCM amplitude {PeakAmplitude}.",
                this.framesWrittenSincePlaybackLog,
                this.waveOut?.PlaybackState,
                this.peakPlaybackAmplitudeSinceLog);
            this.lastPlaybackLogAt = DateTime.UtcNow;
            this.framesWrittenSincePlaybackLog = 0;
            this.peakPlaybackAmplitudeSinceLog = 0;
        }
    }

    public Task StartAudioSink()
    {
        this.waveOut?.Play();
        Logger.LogInformation("Playback device Play() called, PlaybackState is now {PlaybackState}.", this.waveOut?.PlaybackState);
        return Task.CompletedTask;
    }

    public Task CloseAudioSink()
    {
        this.waveOut?.Stop();
        return Task.CompletedTask;
    }

    public Task PauseAudioSink()
    {
        this.isSinkPaused = true;
        this.waveOut?.Pause();
        return Task.CompletedTask;
    }

    public Task ResumeAudioSink()
    {
        this.isSinkPaused = false;
        this.waveOut?.Play();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        this.CloseAudio();
        this.CloseAudioSink();
        this.waveOut?.Dispose();
        this.waveOut = null;
    }
}
