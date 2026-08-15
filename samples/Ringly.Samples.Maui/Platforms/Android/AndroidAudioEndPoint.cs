using System.Net;
using Android.Media;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;
using AndroidAudioFormat = Android.Media.Encoding;
using AudioFormat = SIPSorceryMedia.Abstractions.AudioFormat;

namespace Ringly.Samples.Maui.Platforms.Android;

// No official SIPSorceryMedia.Android package exists (SIPSorceryMedia.Windows has no Android
// counterpart) — this hand-rolls the same IAudioSource/IAudioSink surface WindowsAudioEndPoint
// implements, using Android's native AudioRecord (mic capture) and AudioTrack (speaker
// playback) directly, so the real, cross-device Windows<->Android call path in
// samples/Ringly.Samples.Maui's two-instance demo can carry actual audio on both ends.
public sealed class AndroidAudioEndPoint : IAudioSource, IAudioSink, IDisposable
{
    private const int SampleRate = 8000;
    private const int FrameDurationMs = 20;

    // Jitter buffer tuning: confirmed live over a lossy network path (phone-hosted Wi-Fi
    // hotspot, contending for the same phone's radio/CPU as this call) that writing decoded RTP
    // straight to AudioTrack on arrival — no smoothing at all — produces audible choppiness the
    // instant packets arrive late or out of order, since AudioTrack's own internal buffer only
    // absorbs timing jitter, not genuinely missing frames.
    private const int PrebufferFrameCount = 5; // ~100ms of lookahead before playback starts, to absorb the first burst of jitter
    private const int MaxJitterBufferFrames = 10; // ~200ms cap — beyond this we're falling behind real time, drop oldest to stop latency creeping up call after call
    private const int MaxConcealedFramesInARow = 5; // ~100ms of repeat-last-frame concealment for a real gap before giving up and letting it go quiet, so true silence doesn't get masked as looping noise forever
    private static readonly float[] ConcealmentFadeFactors = [0.65f, 0.4f, 0.22f, 0.1f, 0.04f]; // indexed by concealedFramesInARow — fades toward silence instead of repeating at a flat level, so a real gap reads as a soft dropout rather than a buzzy stutter

    // NOT static readonly: this class is constructed in MauiProgram.cs before builder.Build()
    // runs, which is before SIPSorcery.LogFactory.Set() wires up the real ILoggerFactory — a
    // static readonly field here would permanently capture the no-op default logger at type-init
    // time, silently discarding every log call for the app's entire lifetime regardless of
    // rebuilds. Confirmed live: none of this class's log lines ever appeared despite the
    // corresponding code paths genuinely executing. Fetching fresh each call is cheap and always
    // reflects whatever factory is current by the time real logging happens (well after startup).
    private static ILogger Logger => SIPSorcery.LogFactory.CreateLogger<AndroidAudioEndPoint>();

    private readonly IAudioEncoder audioEncoder;
    private readonly MediaFormatManager<AudioFormat> sourceFormatManager;
    private readonly MediaFormatManager<AudioFormat> sinkFormatManager;
    private readonly object sinkLock = new();

    private AudioRecord? audioRecord;
    private AudioTrack? audioTrack;
    private Thread? captureThread;
    private volatile bool isCapturing;
    private bool isSourcePaused;
    private bool isSinkPaused;

    private int framesWrittenSincePlaybackLog;
    private short peakPlaybackAmplitudeSinceLog;
    private DateTime lastPlaybackLogAt = DateTime.MinValue;
    private DateTime lastPlaybackSkipLogAt = DateTime.MinValue;

    private readonly Queue<short[]> jitterBuffer = new();
    private readonly object jitterBufferLock = new();
    private Thread? playbackFeedThread;
    private volatile bool isPlaybackFeedRunning;
    private short[]? lastPlayedFrame;
    private int concealedFramesInARow;
    private int concealedFramesSinceLog;
    private DateTime lastConcealmentLogAt = DateTime.MinValue;

    public event EncodedSampleDelegate? OnAudioSourceEncodedSample;
    public event Action<EncodedAudioFrame>? OnAudioSourceEncodedFrameReady;

    // This source only ever produces encoded samples (via OnAudioSourceEncodedSample), same as
    // WindowsAudioEndPoint — the raw-sample event exists purely to satisfy the interface.
    public event RawAudioSampleDelegate? OnAudioSourceRawSample;

    public event SourceErrorDelegate? OnAudioSourceError;
    public event SourceErrorDelegate? OnAudioSinkError;

    public AndroidAudioEndPoint(IAudioEncoder audioEncoder)
    {
        this.audioEncoder = audioEncoder;
        this.sourceFormatManager = new MediaFormatManager<AudioFormat>(audioEncoder.SupportedFormats);
        this.sinkFormatManager = new MediaFormatManager<AudioFormat>(audioEncoder.SupportedFormats);

        this.InitPlaybackTrack();
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
        if (this.isCapturing)
        {
            return Task.CompletedTask;
        }

        int minBufferSize = AudioRecord.GetMinBufferSize(SampleRate, ChannelIn.Mono, AndroidAudioFormat.Pcm16bit);

        if (minBufferSize <= 0)
        {
            this.OnAudioSourceError?.Invoke("Unable to determine a valid AudioRecord buffer size for this device.");
            return Task.CompletedTask;
        }

        // VoiceCommunication (Android's dialer/VoIP source, applies echo-cancellation where
        // available) was confirmed live to initialize successfully (State.Initialized) and
        // StartRecording() without throwing, yet AudioRecord.Read() then blocked forever —
        // never returning, not even with zero/silent samples — for the entire duration of a
        // real call on this device. That matches a known OEM-skin restriction on some Android
        // builds: VOICE_COMMUNICATION can require the app to be recognized as the active
        // in-call app via the telecom framework, which this sample never registers as. Mic is
        // the plain, universally-available capture source with no such requirement.
        this.audioRecord = new AudioRecord(
            AudioSource.Mic,
            SampleRate,
            ChannelIn.Mono,
            AndroidAudioFormat.Pcm16bit,
            minBufferSize * 2);

        if (this.audioRecord.State != State.Initialized)
        {
            this.OnAudioSourceError?.Invoke(
                "AudioRecord failed to initialize — RECORD_AUDIO permission may not have been granted.");
            this.audioRecord.Release();
            this.audioRecord = null;
            return Task.CompletedTask;
        }

        this.isCapturing = true;
        this.audioRecord.StartRecording();

        Logger.LogInformation("AudioRecord.StartRecording() called, RecordingState is now {RecordingState}.", this.audioRecord.RecordingState);

        this.captureThread = new Thread(this.CaptureLoop) { IsBackground = true, Name = "AndroidAudioCapture" };
        this.captureThread.Start();

        return Task.CompletedTask;
    }

    private void CaptureLoop()
    {
        int frameSamples = SampleRate * FrameDurationMs / 1000;
        var buffer = new short[frameSamples];

        // Confirmed repeatedly via a live device test: this pipeline reports every step
        // succeeding (AudioRecord.State == Initialized, StartRecording() doesn't throw, the
        // security context comes up cleanly, OnAudioSourceEncodedSample fires) yet Asterisk's
        // own RTP packet counters show ZERO packets ever arriving from this leg. That gap can
        // only be one of: the mic is genuinely capturing silence/nothing despite "success", or
        // packets leave this process but never reach Asterisk over the network. This periodic
        // summary distinguishes the two — reporting real peak amplitude proves the mic itself is
        // working; if that's healthy and Asterisk still sees nothing, the problem is downstream
        // of this class entirely.
        int framesSinceLog = 0;
        int framesSentSinceLog = 0;
        short peakAmplitudeSinceLog = 0;
        DateTime lastSkipLogAt = DateTime.MinValue;
        bool loggedFirstRead = false;

        while (this.isCapturing && this.audioRecord is not null)
        {
            DateTime readStartedAt = DateTime.UtcNow;
            int samplesRead = this.audioRecord.Read(buffer, 0, buffer.Length);

            if (!loggedFirstRead)
            {
                loggedFirstRead = true;
                Logger.LogInformation(
                    "First AudioRecord.Read() returned after {ElapsedMs}ms with samplesRead={SamplesRead}.",
                    (DateTime.UtcNow - readStartedAt).TotalMilliseconds,
                    samplesRead);
            }

            if (samplesRead <= 0 || this.isSourcePaused || this.sourceFormatManager.SelectedFormat.IsEmpty())
            {
                if (DateTime.UtcNow - lastSkipLogAt > TimeSpan.FromSeconds(2))
                {
                    lastSkipLogAt = DateTime.UtcNow;
                    Logger.LogWarning(
                        "Capture frame skipped: samplesRead={SamplesRead}, isSourcePaused={IsSourcePaused}, formatIsEmpty={FormatIsEmpty}.",
                        samplesRead,
                        this.isSourcePaused,
                        this.sourceFormatManager.SelectedFormat.IsEmpty());
                }

                continue;
            }

            short[] pcm = samplesRead == buffer.Length ? buffer : buffer[..samplesRead];

            for (int i = 0; i < pcm.Length; i++)
            {
                short absSample = pcm[i] < 0 ? (short)-pcm[i] : pcm[i];
                if (absSample > peakAmplitudeSinceLog)
                {
                    peakAmplitudeSinceLog = absSample;
                }
            }

            byte[] encodedSample = this.audioEncoder.EncodeAudio(pcm, this.sourceFormatManager.SelectedFormat);
            this.OnAudioSourceEncodedSample?.Invoke((uint)encodedSample.Length, encodedSample);

            framesSentSinceLog++;
            framesSinceLog++;

            if (framesSinceLog >= SampleRate / frameSamples * 2)
            {
                Logger.LogInformation(
                    "Captured {FramesSent} frames in the last ~2s, peak PCM amplitude {PeakAmplitude} (of 32767 max — near 0 means the mic is capturing silence).",
                    framesSentSinceLog,
                    peakAmplitudeSinceLog);
                framesSinceLog = 0;
                framesSentSinceLog = 0;
                peakAmplitudeSinceLog = 0;
            }
        }
    }

    public Task CloseAudio()
    {
        this.isCapturing = false;
        this.captureThread?.Join(TimeSpan.FromMilliseconds(500));
        this.captureThread = null;

        this.audioRecord?.Stop();
        this.audioRecord?.Release();
        this.audioRecord = null;

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

    private void InitPlaybackTrack()
    {
        int minBufferSize = AudioTrack.GetMinBufferSize(SampleRate, ChannelOut.Mono, AndroidAudioFormat.Pcm16bit);

        // STREAM_VOICE_CALL (the legacy constructor this used previously) is reserved for the
        // real telephony/dialer audio HAL — confirmed live that Android/OEM audio policy silently
        // routes it nowhere audible for a non-system, non-telephony app: AudioTrack.Write()
        // succeeded and reported normal state, real non-silent PCM was confirmed arriving via RTP
        // and being written, yet nothing came out of the speaker. This mirrors the earlier
        // AudioSource.VoiceCommunication capture bug on this same class — another legacy
        // telephony-reserved constant that gets silently gated outside the system dialer. The
        // AudioAttributes-based Builder API with Usage=VoiceCommunication is the actual documented
        // path for third-party VoIP apps (used by WhatsApp, Discord, Teams, etc.) and routes to
        // the normal earpiece/speaker output.
        this.audioTrack = new AudioTrack.Builder()
            .SetAudioAttributes(new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.VoiceCommunication)
                .SetContentType(AudioContentType.Speech)
                .Build()!)
            .SetAudioFormat(new global::Android.Media.AudioFormat.Builder()
                .SetEncoding(AndroidAudioFormat.Pcm16bit)
                .SetSampleRate(SampleRate)
                .SetChannelMask(ChannelOut.Mono)
                .Build()!)
            .SetBufferSizeInBytes(Math.Max(minBufferSize, 1) * 2)
            .SetTransferMode(AudioTrackMode.Stream)
            .Build();
    }

    [Obsolete("Use GotEncodedMediaFrame instead.")]
    public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload) =>
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

        // Hand off to the jitter buffer rather than writing straight to AudioTrack here —
        // PlaybackFeedLoop drains this at a steady clocked cadence, decoupling "packets arrive
        // whenever the lossy network delivers them" from "audio must be fed at a steady rate."
        lock (this.jitterBufferLock)
        {
            this.jitterBuffer.Enqueue(pcm);

            while (this.jitterBuffer.Count > MaxJitterBufferFrames)
            {
                this.jitterBuffer.Dequeue();
            }
        }
    }

    // Runs at a steady 20ms cadence (clocked off a Stopwatch, not sleep-accumulated, so it
    // doesn't drift) for as long as the sink is active, pulling decoded frames from the jitter
    // buffer and writing them to AudioTrack. When the buffer is empty because a real packet was
    // lost or delayed, it conceals the gap by repeating the last frame (attenuated) for a short
    // window instead of underrunning AudioTrack or leaving dead air.
    private void PlaybackFeedLoop()
    {
        DateTime prebufferDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (this.isPlaybackFeedRunning && DateTime.UtcNow < prebufferDeadline)
        {
            lock (this.jitterBufferLock)
            {
                if (this.jitterBuffer.Count >= PrebufferFrameCount)
                {
                    break;
                }
            }

            Thread.Sleep(5);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        long nextFrameDueAtMs = 0;

        while (this.isPlaybackFeedRunning)
        {
            short[]? frame = null;

            lock (this.jitterBufferLock)
            {
                if (this.jitterBuffer.Count > 0)
                {
                    frame = this.jitterBuffer.Dequeue();
                }
            }

            if (frame is not null)
            {
                this.lastPlayedFrame = frame;
                this.concealedFramesInARow = 0;
                this.WriteFrameToAudioTrack(frame);
            }
            else if (this.lastPlayedFrame is not null && this.concealedFramesInARow < MaxConcealedFramesInARow)
            {
                // Repeating the exact same waveform verbatim creates an audible click/buzz at the
                // seam each time it happens — confirmed live that with frequent-but-small losses
                // (a few frames almost every 2s window on a real network), that added up to
                // "keeps breaking, can't make out what's coming back" even though the raw loss
                // rate was modest. Fading each successive concealed frame toward silence instead
                // of repeating at a constant level reads as a soft dropout rather than a stutter.
                this.WriteFrameToAudioTrack(AttenuateFrame(this.lastPlayedFrame, ConcealmentFadeFactors[this.concealedFramesInARow]));
                this.concealedFramesInARow++;

                this.concealedFramesSinceLog++;
                if (DateTime.UtcNow - this.lastConcealmentLogAt > TimeSpan.FromSeconds(2))
                {
                    Logger.LogWarning(
                        "Concealed {ConcealedFrames} missing playback frame(s) in the last ~2s by repeating the last received frame (network jitter/loss).",
                        this.concealedFramesSinceLog);
                    this.lastConcealmentLogAt = DateTime.UtcNow;
                    this.concealedFramesSinceLog = 0;
                }
            }

            nextFrameDueAtMs += FrameDurationMs;
            int sleepMs = (int)(nextFrameDueAtMs - stopwatch.ElapsedMilliseconds);

            if (sleepMs > 0)
            {
                Thread.Sleep(sleepMs);
            }
            else
            {
                // Fell behind real time (e.g. a GC pause) — resync instead of catching up by
                // writing frames back-to-back, which would sound like fast-forwarding.
                nextFrameDueAtMs = stopwatch.ElapsedMilliseconds;
            }
        }
    }

    private void WriteFrameToAudioTrack(short[] pcm)
    {
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
            this.audioTrack?.Write(pcm, 0, pcm.Length);
        }

        this.framesWrittenSincePlaybackLog++;

        if (DateTime.UtcNow - this.lastPlaybackLogAt > TimeSpan.FromSeconds(2))
        {
            Logger.LogInformation(
                "Wrote {FramesWritten} playback frames in the last ~2s to AudioTrack (PlayState={PlayState}), peak PCM amplitude {PeakAmplitude}.",
                this.framesWrittenSincePlaybackLog,
                this.audioTrack?.PlayState,
                this.peakPlaybackAmplitudeSinceLog);
            this.lastPlaybackLogAt = DateTime.UtcNow;
            this.framesWrittenSincePlaybackLog = 0;
            this.peakPlaybackAmplitudeSinceLog = 0;
        }
    }

    private static short[] AttenuateFrame(short[] source, float factor)
    {
        var result = new short[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = (short)(source[i] * factor);
        }

        return result;
    }

    public Task StartAudioSink()
    {
        // Pairs with AudioAttributes.Usage=VoiceCommunication on the AudioTrack itself (see
        // InitPlaybackTrack): MODE_IN_COMMUNICATION is what tells Android's audio policy this is
        // an active VoIP call, which is what actually engages correct earpiece/speaker routing for
        // a VoiceCommunication-usage stream on several OEMs (confirmed necessary in addition to
        // the AudioTrack attributes alone during live testing on a Samsung device).
        try
        {
            using var audioManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.AudioService) as AudioManager;
            if (audioManager is not null)
            {
                audioManager.Mode = Mode.InCommunication;
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to set AudioManager.Mode to InCommunication.");
        }

        this.audioTrack?.Play();

        lock (this.jitterBufferLock)
        {
            this.jitterBuffer.Clear();
        }

        this.lastPlayedFrame = null;
        this.concealedFramesInARow = 0;
        this.isPlaybackFeedRunning = true;
        this.playbackFeedThread = new Thread(this.PlaybackFeedLoop) { IsBackground = true, Name = "AndroidAudioPlaybackFeed" };
        this.playbackFeedThread.Start();

        return Task.CompletedTask;
    }

    public Task CloseAudioSink()
    {
        this.isPlaybackFeedRunning = false;
        this.playbackFeedThread?.Join(TimeSpan.FromMilliseconds(500));
        this.playbackFeedThread = null;

        this.audioTrack?.Stop();

        try
        {
            using var audioManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.AudioService) as AudioManager;
            if (audioManager is not null)
            {
                audioManager.Mode = Mode.Normal;
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to reset AudioManager.Mode to Normal.");
        }

        return Task.CompletedTask;
    }

    public Task PauseAudioSink()
    {
        this.isSinkPaused = true;
        this.audioTrack?.Pause();
        return Task.CompletedTask;
    }

    public Task ResumeAudioSink()
    {
        this.isSinkPaused = false;
        this.audioTrack?.Play();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        this.CloseAudio();
        this.CloseAudioSink();

        lock (this.sinkLock)
        {
            this.audioTrack?.Release();
            this.audioTrack = null;
        }
    }
}
