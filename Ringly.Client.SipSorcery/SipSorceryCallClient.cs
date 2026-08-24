using System.Net;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ringly.Client.Abstractions;
using Ringly.Client.Abstractions.Models;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions;

namespace Ringly.Client.SipSorcery;

public class SipSorceryCallClient : ICallClient, IDisposable
{
    // Not static readonly — confirmed live in AndroidAudioEndPoint that a static readonly
    // logger field can permanently capture SIPSorcery's default no-op logger if this class is
    // ever constructed before SIPSorcery.LogFactory.Set() wires up the real ILoggerFactory,
    // silently discarding every log call for the rest of the app's lifetime. This class is
    // currently only ever resolved lazily via DI, well after that point, but fetching fresh each
    // call costs nothing and removes any doubt.
    private static ILogger Logger => SIPSorcery.LogFactory.CreateLogger<SipSorceryCallClient>();

    private readonly SipSorceryCallOptions options;
    private readonly SIPTransport sipTransport;
    private readonly SIPUserAgent userAgent;
    private readonly Subject<CallClientEvent> events;
    private readonly Dictionary<string, SIPServerUserAgent> pendingIncomingCalls;
    private readonly Dictionary<string, RTCPeerConnection> activeMediaSessions;
    private readonly RTCConfiguration rtcConfiguration;
    private readonly IAudioSource? audioSource;
    private readonly IAudioSink? audioSink;
    private readonly IVideoSource? videoSource;
    private readonly IVideoSink? videoSink;
    private SipCredentials? registeredCredentials;
    private SIPRegistrationUserAgent? registrationAgent;

    // audioSource/audioSink (and videoSource/videoSink, same idiom) are optional and
    // platform-specific — this library has no built-in microphone/speaker/camera access of its
    // own (SIPSorcery's own platform audio packages, e.g. SIPSorceryMedia.Windows, need a
    // platform-specific TFM this cross-platform project doesn't have). Without them, calls still
    // negotiate real SDP and connect (proven working), but no actual audio/video flows. Callers that
    // want real media construct platform endpoints (e.g. SIPSorceryMedia.Windows's
    // WindowsAudioEndPoint implements both audio interfaces) and register them for DI;
    // Microsoft.Extensions.DependencyInjection resolves these to null via the default parameter
    // when nothing's registered, so this stays a no-op on platforms that don't provide one.
    public SipSorceryCallClient(
        IOptions<SipSorceryCallOptions> options,
        IAudioSource? audioSource = null,
        IAudioSink? audioSink = null,
        IVideoSource? videoSource = null,
        IVideoSink? videoSink = null)
    {
        this.audioSource = audioSource;
        this.audioSink = audioSink;
        this.videoSource = videoSource;
        this.videoSink = videoSink;
        this.options = options.Value;
        this.events = new Subject<CallClientEvent>();
        this.pendingIncomingCalls = [];
        this.activeMediaSessions = [];

        // RtpIceChannel's defaults (8s to declare "disconnected", 16s to declare hard "failed")
        // are tuned for stable networks — confirmed via a live real-device test that ordinary
        // Wi-Fi jitter is enough to trip these: Asterisk's own trace showed the callee's channel
        // joining the bridge and then leaving again within moments, matching these exact
        // timings, well after ICE, DTLS-SRTP, and the firewall/NAT/coturn path had all already
        // been individually confirmed working. These are mutable public static fields (not
        // consts) specifically so callers can raise the tolerance; done once here for the whole
        // app rather than per-call, since there's no legitimate reason to want the aggressive
        // defaults for this client.
        //
        // Raised again (30s -> 90s, 60s -> 120s): confirmed live a second time — real calls
        // were still being torn down at almost exactly 30.0s post-answer (measured from
        // Asterisk's own trace: customer channel answer to client-initiated BYE), the previous
        // raised value, on a different Wi-Fi network than the one that prompted the original
        // 8s->30s change. The jitter this is compensating for isn't a one-network fluke.
        SIPSorcery.Net.RtpIceChannel.DISCONNECTED_TIMEOUT_PERIOD = 90;
        SIPSorcery.Net.RtpIceChannel.FAILED_TIMEOUT_PERIOD = 120;

        this.rtcConfiguration = new RTCConfiguration
        {
            iceServers = this.options.IceServerUrls
                .Select(url => new RTCIceServer
                {
                    urls = url,
                    username = this.options.IceServerUsername,
                    credential = this.options.IceServerCredential,
                    credentialType = RTCIceCredentialType.password
                })
                .ToList(),

            // See SipSorceryCallOptions.BindAddress's own comment for why this is needed — without
            // it, ICE can gather and the remote peer can nominate a host candidate on a
            // Windows-host-only virtual adapter (e.g. Docker Desktop's WSL2 bridge) that happens to
            // share a subnet with this project's own docker-compose network, silently timing out
            // DTLS since nothing on the other device can ever reach it.
            X_BindAddress = string.IsNullOrWhiteSpace(this.options.BindAddress)
                ? null
                : System.Net.IPAddress.Parse(this.options.BindAddress)

            // NOTE: forcing iceTransportPolicy = relay was tried here to work around a direct
            // host/peer-reflexive ICE pair silently carrying zero real media despite reporting
            // "connected" (see strictrtp=no in docker/asterisk/config/rtp.conf for the sibling
            // fix to a related issue). Reverted — confirmed live that requiring relay-only
            // removes any fallback, so a single TURN allocate failure (observed: STUN error 437,
            // "Allocation Mismatch") now fails the call outright with no candidates left to try
            // at all, which is strictly worse than the original problem. Revisit once the 437
            // itself is root-caused (coturn state left over from repeated rapid test attempts is
            // one live theory) rather than reintroducing this as a blanket policy.
        };

        this.sipTransport = new SIPTransport();
        this.sipTransport.AddSIPChannel(new SIPClientWebSocketChannel());

        // SIPSorcery's SIPClientWebSocketChannel always connects to the WS server's root path
        // ("/") with no way to target a sub-path — confirmed in its source (SendAsync/
        // SendSecureAsync build the URI purely from "ws(s)://{endpoint}", never a path).
        // Asterisk's WebSocket transport is hardcoded to "/ws" (res_http_websocket), so pure
        // WS/WSS registrations against Asterisk 404 outright; there is no RegistrarHost format
        // that reconciles the two. UDP has no such path concept, so it's added as a fallback
        // transport — set RegistrarHost without ";transport=ws(s)" (or with ";transport=udp")
        // to use it, which is what native clients (unlike browsers, which lack raw socket
        // access and need WS) should generally prefer against Asterisk anyway.
        this.sipTransport.AddSIPChannel(new SIPUDPChannel(IPAddress.Any, 0));

        // isTransportExclusive: true — this client owns sipTransport exclusively (created it
        // above, nothing else shares it). Confirmed via SIPUserAgent's source that in
        // non-exclusive mode (the constructor's default), inbound SIP *requests* (INVITE,
        // OPTIONS, etc.) still raise OnIncomingCall, but no automatic response is ever sent back
        // over the wire — matching the exact symptom observed: registration (which only relies
        // on matching responses to our own outbound requests) worked fine, while ANY inbound
        // request, including a raw hand-crafted OPTIONS probe, got zero reply despite the socket
        // being confirmed bound and receiving traffic at the OS level.
        this.userAgent = new SIPUserAgent(this.sipTransport, null, isTransportExclusive: true);

        this.userAgent.OnIncomingCall += this.HandleIncomingCall;
        this.userAgent.OnDtmfTone += this.HandleDtmfTone;
        this.userAgent.OnCallHungup += this.HandleCallHungup;
        this.userAgent.ClientCallAnswered += this.HandleClientCallAnswered;
        this.userAgent.ClientCallFailed += this.HandleClientCallFailed;
    }

    public ValueTask RegisterAsync(SipCredentials credentials)
    {
        // Calling this again (e.g. the user tapping "Register" a second time) used to just
        // create a brand new SIPRegistrationUserAgent on top of whatever was already running,
        // leaking the old one — its background refresh timer kept firing indefinitely,
        // completely untracked. Confirmed as a real bug against a live Asterisk instance: with
        // two of these running for the same extension, periodic re-registration intermittently
        // failed with "Registration unequivocal failure with Unauthorised... no further
        // registration attempts will be made" (a stale/colliding digest nonce between the two
        // agents' independent refresh cycles), silently leaving the client's actual transport
        // in a broken state — even though Asterisk's own contact table still showed a
        // seemingly-live registration from whichever agent last succeeded. Stopping any
        // previous agent first means there's only ever one active per client instance.
        this.registrationAgent?.Stop(sendZeroExpiryRegister: false);

        var registrationCompletionSource = new TaskCompletionSource();

        var registrationAgent = new SIPRegistrationUserAgent(
            this.sipTransport,
            credentials.Extension,
            credentials.Password,
            this.options.RegistrarHost,
            this.options.RegistrationExpirySeconds);

        this.registrationAgent = registrationAgent;

        registrationAgent.RegistrationSuccessful += (uri, response) =>
        {
            this.registeredCredentials = credentials;
            registrationCompletionSource.TrySetResult();
        };

        registrationAgent.RegistrationFailed += (uri, response, errorMessage) =>
            registrationCompletionSource.TrySetException(new InvalidOperationException(errorMessage));

        registrationAgent.Start();

        return new ValueTask(registrationCompletionSource.Task);
    }

    public async ValueTask<CallHandle> PlaceCallAsync(string targetExtension, bool includeVideo = true)
    {
        var mediaSession = this.CreateMediaSession(includeVideo);

        // A bare extension (e.g. "1001") is not a resolvable SIP destination on its own — with
        // no "@domain" part, SIPSorcery's URI resolution falls back to legacy dotted-decimal
        // integer parsing (observed: "1001" resolved to IP 0.0.3.233, since 1001 as a raw
        // 32-bit value is 0.0.3.233 in dotted-decimal), then just times out. Qualifying it
        // against the same host used for registration routes it through Asterisk correctly.
        string registrarHost = this.options.RegistrarHost.Split(';')[0];
        string destination = $"sip:{targetExtension}@{registrarHost}";

        // Asterisk challenges the INVITE for authentication the same way it challenges REGISTER
        // (confirmed: an unauthenticated Call() gets "Authentication requested when no
        // credentials available") — reusing the credentials this client last registered with
        // answers that challenge the same way SIPRegistrationUserAgent does internally.
        bool callResult = await this.userAgent.Call(
            destination,
            username: this.registeredCredentials?.Extension,
            password: this.registeredCredentials?.Password,
            mediaSession);

        if (!callResult)
        {
            mediaSession.Close("call failed");
            throw new InvalidOperationException($"Failed to place call to {targetExtension}.");
        }

        var handle = new CallHandle { Id = Guid.NewGuid().ToString() };
        this.activeMediaSessions[handle.Id] = mediaSession;

        return handle;
    }

    public async ValueTask AnswerCallAsync(CallHandle handle)
    {
        if (!this.pendingIncomingCalls.Remove(handle.Id, out SIPServerUserAgent? uas))
        {
            throw new InvalidOperationException($"No pending incoming call for handle '{handle.Id}'.");
        }

        var mediaSession = this.CreateMediaSession();
        await this.userAgent.Answer(uas, mediaSession);
        this.activeMediaSessions[handle.Id] = mediaSession;
    }

    public ValueTask HangupAsync(CallHandle handle)
    {
        // An incoming call that hasn't been answered yet (Decline) has no established
        // SIPUserAgent dialogue for userAgent.Hangup() to act on — confirmed live that calling
        // it in that case was a silent no-op: nothing was ever sent back to the caller at all,
        // leaving their UI stuck showing "dialing" forever with no failure response to react to.
        // The still-pending SIPServerUserAgent has to be rejected directly instead, which is
        // what actually sends the SIP response (486 Busy Here) the caller's SIPClientUserAgent
        // needs to fail the call and update its own UI.
        if (this.pendingIncomingCalls.Remove(handle.Id, out SIPServerUserAgent? uas))
        {
            try
            {
                uas.Reject(SIPResponseStatusCodesEnum.BusyHere, "Declined");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "SIPServerUserAgent.Reject() failed for call {CallHandleId}.", handle.Id);
            }

            return ValueTask.CompletedTask;
        }

        // SIPUserAgent.Hangup() closes its own tracked MediaSession *before* sending BYE — if
        // that throws (or BYE construction/sending itself throws), the exception previously
        // propagated out completely unlogged here, silently skipping BYE entirely while still
        // leaving the caller's own eventLog (UI-only, easy to miss) as the only trace. Confirmed
        // via a live test that the far end never receives a hangup signal in this scenario —
        // this makes any such failure visible in the persistent debug log instead.
        try
        {
            this.userAgent.Hangup();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "SIPUserAgent.Hangup() failed for call {CallHandleId}.", handle.Id);
        }

        if (this.activeMediaSessions.Remove(handle.Id, out RTCPeerConnection? mediaSession) && !mediaSession.IsClosed)
        {
            mediaSession.Close("hangup");
        }

        return ValueTask.CompletedTask;
    }

    // Same registrar-host-qualification reasoning as PlaceCallAsync — a bare extension has no
    // resolvable "@domain" part on its own. Sends a real SIP REFER to the current call's own
    // dialog peer (e.g. Asterisk), asking it to dial targetExtension and take over from here;
    // SIPSorcery's own BlindTransfer acts on this client's single tracked SIPUserAgent dialogue,
    // so handle isn't otherwise used - kept on the signature to match every other call-scoped
    // method here and to fail loudly (via SIPUserAgent's own null-dialogue behavior) rather than
    // silently transferring the wrong call if this client ever tracks more than one at once.
    public async ValueTask<bool> BlindTransferAsync(CallHandle handle, string targetExtension, int timeoutSeconds = 10)
    {
        string registrarHost = this.options.RegistrarHost.Split(';')[0];
        SIPURI destinationUri = SIPURI.ParseSIPURIRelaxed($"{targetExtension}@{registrarHost}");

        return await this.userAgent.BlindTransfer(
            destinationUri, TimeSpan.FromSeconds(timeoutSeconds), CancellationToken.None);
    }

    // No-op without a real audioSource (e.g. Android, which has none wired up yet) — there's
    // no audio being sent to pause in the first place.
    public async ValueTask MuteAsync()
    {
        if (this.audioSource is not null)
        {
            await this.audioSource.PauseAudio();
        }
    }

    public async ValueTask UnmuteAsync()
    {
        if (this.audioSource is not null)
        {
            await this.audioSource.ResumeAudio();
        }
    }

    public async ValueTask MuteVideoAsync()
    {
        if (this.videoSource is not null)
        {
            await this.videoSource.PauseVideo();
        }
    }

    public async ValueTask UnmuteVideoAsync()
    {
        if (this.videoSource is not null)
        {
            await this.videoSource.ResumeVideo();
        }
    }

    public IObservable<CallClientEvent> StreamEvents() => this.events;

    public void Dispose()
    {
        this.registrationAgent?.Stop(sendZeroExpiryRegister: false);

        this.userAgent.OnIncomingCall -= this.HandleIncomingCall;
        this.userAgent.OnDtmfTone -= this.HandleDtmfTone;
        this.userAgent.OnCallHungup -= this.HandleCallHungup;
        this.userAgent.ClientCallAnswered -= this.HandleClientCallAnswered;
        this.userAgent.ClientCallFailed -= this.HandleClientCallFailed;

        foreach (RTCPeerConnection mediaSession in this.activeMediaSessions.Values)
        {
            mediaSession.Close("disposed");
        }

        this.activeMediaSessions.Clear();

        this.userAgent.Close();
        this.sipTransport.Shutdown();
        this.events.Dispose();
    }

    // includeVideo=false is how PlaceCallAsync's own includeVideo option skips offering a video
    // track for a deliberately audio-only call — see AnswerCallAsync's call site for why it
    // always passes the default true regardless: the answer side's video track only actually
    // negotiates against whatever "m=video" line the remote OFFER contains in the first place, so
    // there's no separate audio-only toggle needed there.
    private RTCPeerConnection CreateMediaSession(bool includeVideo = true)
    {
        var mediaSession = new RTCPeerConnection(this.rtcConfiguration);

        // Without a track, the generated SDP has no "m=" line at all — confirmed against a
        // real Asterisk instance: an offer of just "v=0/o=.../s=sipsorcery/t=0 0" with no media
        // description is correctly rejected with "488 Not Acceptable Here" (there's nothing to
        // negotiate). When a real audioSource is available, advertise what it can actually
        // produce; otherwise fall back to declaring PCMU alone — enough for SDP to negotiate,
        // but with no real audio behind it (signaling-only, confirmed working end to end without
        // an audioSource/audioSink).
        List<AudioFormat> formats = this.audioSource?.GetAudioSourceFormats()
            ?? [new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMU)];

        var audioTrack = new MediaStreamTrack(formats, MediaStreamStatusEnum.SendRecv);
        mediaSession.addTrack(audioTrack);

        // Confirmed live as the actual cause of the ~30-35s call cutoff that survived raising
        // RtpIceChannel's own DISCONNECTED_TIMEOUT_PERIOD/FAILED_TIMEOUT_PERIOD (a separate,
        // ICE-only mechanism that never fired here at all): SIPSorcery's RTCPSession has its own
        // independent no-activity watchdog (NoActivityTimeoutMilliseconds, default 30000ms,
        // checked every 5s) that calls SIPUserAgent.Hangup() - a real SIP BYE, not just an ICE
        // state change - the moment a media stream goes 30s without receiving an RTP or RTCP
        // packet. Confirmed via Asterisk's own SIP trace: the BYE that ends every call arrives
        // 30-35s after answer, from this client, on the same dialog - matching this timer's
        // 30000ms threshold plus up to 5s until its next check tick, not the ICE timers at all.
        // First raised to 90000ms to match ICE's own disconnect tolerance - confirmed live a
        // second time that this only delayed the same cutoff (a call ran ~88s, not indefinitely),
        // meaning real RTP/RTCP genuinely does go quiet mid-call on a healthy, still-connected
        // call (e.g. codec silence suppression/DTX, or a transient NAT/relay gap) - raising the
        // number further only postpones the same false-positive hangup. Disabled outright:
        // Ringly's own call-lifecycle contract is that a call ends only on an explicit hangup
        // (UI button or provider-driven), never on an inferred media gap.
        mediaSession.AudioStream.RtcpSession.NoActivityTimeoutMilliseconds = int.MaxValue;

        // Unlike audio, video is only advertised when a real source or sink is actually
        // registered — there's no "signaling-only" fallback video format the way PCMU serves for
        // audio, and forcing a video "m=" line onto every audio-only call would negotiate a
        // capability nothing behind it can use.
        if (includeVideo && (this.videoSource is not null || this.videoSink is not null))
        {
            List<VideoFormat> videoFormats = this.videoSource?.GetVideoSourceFormats()
                ?? this.videoSink!.GetVideoSinkFormats();

            var videoTrack = new MediaStreamTrack(videoFormats, MediaStreamStatusEnum.SendRecv);
            mediaSession.addTrack(videoTrack);
            mediaSession.VideoStream.RtcpSession.NoActivityTimeoutMilliseconds = int.MaxValue;
        }

        // Fallback so the UI always learns a call ended, even when the far end's BYE never
        // arrives — confirmed on a lossy network path that the outgoing BYE itself can time out
        // silently (SIPClientUserAgent logs "Bye request ... failed with TimedOut" but otherwise
        // does nothing further), so ua.OnCallHungup (which only fires from a genuine SIP-level
        // BYE) never runs on the far end and its "CallHungup" event, which HandleCallEvent
        // depends on to reset the UI, never gets published — leaving that side showing "in call"
        // indefinitely even once the media has genuinely died and this side's own ICE-inactivity
        // watchdog has already torn the session down. Guarded to publish once per session since
        // a normal Hangup() also closes the media session and would otherwise double-publish.
        bool hungupEventPublished = false;
        mediaSession.onconnectionstatechange += state =>
        {
            if (!hungupEventPublished && state is RTCPeerConnectionState.closed
                or RTCPeerConnectionState.failed
                or RTCPeerConnectionState.disconnected)
            {
                hungupEventPublished = true;
                this.PublishEvent("CallHungup", new CallHandle());
            }
        };

        if (this.audioSource is not null)
        {
            // Confirmed via a live Android test: mediaSession.SendAudio gets called throughout an
            // entire call's duration (not just briefly at the start) with SIPSorcery repeatedly
            // logging "SendRtpPacket cannot be called on a secure session before calling
            // SetSecurityContext" — despite its own DTLS handshake code applying the security
            // context synchronously before "connected" fires. This wrapper directly observes
            // mediaSession's own security-context readiness (via the same public API SIPSorcery
            // itself checks) at the moment of every send attempt, to get a definitive answer on
            // whether it's ever actually ready during the call, rather than guessing further.
            bool loggedNotReady = false;
            bool loggedReady = false;

            EncodedSampleDelegate sendAudioHandler = (durationRtpUnits, sample) =>
            {
                bool isReady = mediaSession.AudioStream?.GetSecurityContext() is not null;

                if (!isReady && !loggedNotReady)
                {
                    loggedNotReady = true;
                    Logger.LogWarning(
                        "SendAudio observed AudioStream security context NOT ready (connectionState={ConnectionState}).",
                        mediaSession.connectionState);
                }
                else if (isReady && !loggedReady)
                {
                    loggedReady = true;
                    Logger.LogInformation("SendAudio observed AudioStream security context became ready.");
                }

                mediaSession.SendAudio(durationRtpUnits, sample);
            };

            this.audioSource.OnAudioSourceEncodedSample += sendAudioHandler;

            // IAudioSource.StartAudio() doesn't throw on failure — it raises this event instead
            // (e.g. AndroidAudioEndPoint's AudioRecord failing to initialize on a device/emulator
            // with no usable mic, or a permission grant that didn't actually take). Without this
            // subscribed, that failure was completely silent: "Audio source started." still logs
            // (the method returned normally), no exception anywhere, and the only visible symptom
            // was Asterisk's own RTP counters showing zero packets ever received from that leg.
            this.audioSource.OnAudioSourceError += errorMessage =>
                Logger.LogError("Audio source error: {ErrorMessage}", errorMessage);

            mediaSession.OnAudioFormatsNegotiated += negotiatedFormats =>
                this.audioSource.SetAudioSourceFormat(negotiatedFormats.First());

            mediaSession.onconnectionstatechange += async state =>
            {
                // onconnectionstatechange is invoked from SIPSorcery's own internal event
                // dispatch, which does not await or observe this handler — an unlogged exception
                // here (e.g. WindowsAudioEndPoint.StartAudio() failing to open a capture device)
                // would silently vanish, leaving DTLS-SRTP fully connected but zero RTP ever
                // actually sent, indistinguishable from a networking problem without this.
                try
                {
                    Logger.LogInformation("Media session connection state changed to {State}.", state);

                    if (state == RTCPeerConnectionState.connected)
                    {
                        await this.audioSource.StartAudio();
                        Logger.LogInformation("Audio source started.");
                    }
                    else if (state is RTCPeerConnectionState.closed
                        or RTCPeerConnectionState.failed
                        or RTCPeerConnectionState.disconnected)
                    {
                        // this.audioSource is a shared singleton reused across every call (the
                        // platform mic/speaker device is only ever created once) — without this,
                        // OnAudioSourceEncodedSample keeps accumulating one handler per past call
                        // forever, each still bound to that call's now-closed mediaSession. Every
                        // subsequent captured audio frame then fires ALL of them, including the
                        // stale ones, which SIPSorcery correctly rejects — but the resulting
                        // "cannot be called on a secure session"/"closed RTP session" warning
                        // spam drowns out genuine signal from whatever call is actually active.
                        this.audioSource.OnAudioSourceEncodedSample -= sendAudioHandler;
                        await this.audioSource.CloseAudio();
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed handling media session connection state change to {State}.", state);
                }
            };
        }

        if (this.audioSink is not null)
        {
            this.audioSink.OnAudioSinkError += errorMessage =>
                Logger.LogError("Audio sink error: {ErrorMessage}", errorMessage);

            // Confirmed live as the actual reason a real Android device never played back
            // anything despite receiving genuine RTP: GotAudioRtp (the obsolete overload this
            // client calls below) decodes using audioSink's OWN internal format manager, which
            // only ever gets populated by an explicit SetAudioSinkFormat call — never wired up
            // here previously. WindowsAudioEndPoint happened to mask this by auto-initializing
            // its sink format in its own constructor when the encoder supports exactly one
            // format; AndroidAudioEndPoint has no such self-initialization, so its sink format
            // stayed permanently empty and WritePlayback's format.IsEmpty() guard silently
            // discarded every incoming packet for the entire session, no error, no log, nothing.
            mediaSession.OnAudioFormatsNegotiated += negotiatedFormats =>
                this.audioSink.SetAudioSinkFormat(negotiatedFormats.First());

            mediaSession.OnRtpPacketReceived += (remoteEndPoint, mediaType, rtpPacket) =>
            {
                if (mediaType == SDPMediaTypesEnum.audio)
                {
                    this.audioSink.GotAudioRtp(
                        remoteEndPoint,
                        rtpPacket.Header.SyncSource,
                        rtpPacket.Header.SequenceNumber,
                        rtpPacket.Header.Timestamp,
                        rtpPacket.Header.PayloadType,
                        rtpPacket.Header.MarkerBit == 1,
                        rtpPacket.Payload);
                }
            };

            // Received RTP is decoded and buffered into the sink's wave provider regardless
            // (GotAudioRtp above), but nothing is ever actually played out loud until the sink's
            // own playback device is started — confirmed via SIPSorceryMedia.Windows's
            // WindowsAudioEndPoint source: StartAudio() (audioSource, above) only starts the
            // *capture* device (_waveInEvent); playback only begins once StartAudioSink() calls
            // _waveOutEvent.Play() separately. Without this, both call legs fully connect
            // end-to-end (ICE, DTLS-SRTP, RTP flowing) with neither side ever hearing anything.
            mediaSession.onconnectionstatechange += async state =>
            {
                try
                {
                    if (state == RTCPeerConnectionState.connected)
                    {
                        await this.audioSink.StartAudioSink();
                        Logger.LogInformation("Audio sink started.");
                    }
                    else if (state is RTCPeerConnectionState.closed
                        or RTCPeerConnectionState.failed
                        or RTCPeerConnectionState.disconnected)
                    {
                        await this.audioSink.CloseAudioSink();
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed handling audio sink state change to {State}.", state);
                }
            };
        }

        if (this.videoSource is not null)
        {
            EncodedSampleDelegate sendVideoHandler = (durationRtpUnits, sample) =>
                mediaSession.SendVideo(durationRtpUnits, sample);

            this.videoSource.OnVideoSourceEncodedSample += sendVideoHandler;

            this.videoSource.OnVideoSourceError += errorMessage =>
                Logger.LogError("Video source error: {ErrorMessage}", errorMessage);

            mediaSession.OnVideoFormatsNegotiated += negotiatedFormats =>
                this.videoSource.SetVideoSourceFormat(negotiatedFormats.First());

            mediaSession.onconnectionstatechange += async state =>
            {
                try
                {
                    if (state == RTCPeerConnectionState.connected)
                    {
                        await this.videoSource.StartVideo();
                        Logger.LogInformation("Video source started.");
                    }
                    else if (state is RTCPeerConnectionState.closed
                        or RTCPeerConnectionState.failed
                        or RTCPeerConnectionState.disconnected)
                    {
                        // Same reasoning as audioSource's equivalent unsubscribe above — videoSource
                        // is a shared singleton reused across calls (the platform camera is only
                        // ever created once), so leaving this handler attached after the session
                        // closes would accumulate one stale handler per past call.
                        this.videoSource.OnVideoSourceEncodedSample -= sendVideoHandler;
                        await this.videoSource.CloseVideo();
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed handling video source state change to {State}.", state);
                }
            };
        }

        if (this.videoSink is not null)
        {
            mediaSession.OnVideoFormatsNegotiated += negotiatedFormats =>
                this.videoSink.SetVideoSinkFormat(negotiatedFormats.First());

            // Unlike audio (which goes through OnRtpPacketReceived + the obsolete GotAudioRtp
            // overload, since that's the only path AndroidAudioEndPoint/CustomWindowsAudioEndPoint
            // were written against), RTCPeerConnection exposes a higher-level OnVideoFrameReceived
            // event whose signature already matches IVideoSink.GotVideoFrame exactly — no manual
            // RTP-header unpacking needed here.
            mediaSession.OnVideoFrameReceived += this.videoSink.GotVideoFrame;

            mediaSession.onconnectionstatechange += async state =>
            {
                try
                {
                    if (state == RTCPeerConnectionState.connected)
                    {
                        await this.videoSink.StartVideoSink();
                        Logger.LogInformation("Video sink started.");
                    }
                    else if (state is RTCPeerConnectionState.closed
                        or RTCPeerConnectionState.failed
                        or RTCPeerConnectionState.disconnected)
                    {
                        await this.videoSink.CloseVideoSink();
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed handling video sink state change to {State}.", state);
                }
            };
        }

        return mediaSession;
    }

    private void HandleIncomingCall(SIPUserAgent ua, SIPRequest sipRequest)
    {
        SIPServerUserAgent uas = ua.AcceptCall(sipRequest);
        var handle = new CallHandle { Id = Guid.NewGuid().ToString() };
        this.pendingIncomingCalls[handle.Id] = uas;

        // AnswerCallAsync always answers with a video track available when a videoSource/videoSink
        // is registered (see CreateMediaSession's own comment on why includeVideo=true there
        // regardless), so whether the resulting call actually carries video is decided entirely by
        // the caller's OFFER — inspected directly here, at the moment it arrives, so the UI can show
        // the right incoming-call affordance (and later, once answered, the right in-call controls)
        // without guessing.
        bool includesVideo = false;

        try
        {
            includesVideo = SDP.ParseSDPDescription(sipRequest.Body).Media
                .Any(mediaAnnouncement => mediaAnnouncement.Media == SDPMediaTypesEnum.video);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to parse incoming call offer SDP for video capability.");
        }

        this.PublishEvent("IncomingCall", handle, sipRequest.Header.From.FromURI.User, includesVideo);
    }

    private void HandleDtmfTone(byte tone, int durationMs) =>
        this.PublishEvent($"Dtmf:{tone}", new CallHandle());

    private void HandleCallHungup(SIPDialogue dialogue) =>
        this.PublishEvent("CallHungup", new CallHandle());

    private void HandleClientCallAnswered(ISIPClientUserAgent uac, SIPResponse sipResponse) =>
        this.PublishEvent("CallAnswered", new CallHandle());

    private void HandleClientCallFailed(ISIPClientUserAgent uac, string errorMessage, SIPResponse sipResponse) =>
        this.PublishEvent("CallFailed", new CallHandle());

    private void PublishEvent(string eventType, CallHandle handle, string remoteExtension = "", bool includesVideo = false) =>
        this.events.OnNext(new CallClientEvent
        {
            EventType = eventType,
            Handle = handle,
            OccurredDate = DateTimeOffset.UtcNow,
            RemoteExtension = remoteExtension,
            IncludesVideo = includesVideo
        });
}
