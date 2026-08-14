using System.Net;
using System.Reactive.Subjects;
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
    private readonly SipSorceryCallOptions options;
    private readonly SIPTransport sipTransport;
    private readonly SIPUserAgent userAgent;
    private readonly Subject<CallClientEvent> events;
    private readonly Dictionary<string, SIPServerUserAgent> pendingIncomingCalls;
    private readonly Dictionary<string, RTCPeerConnection> activeMediaSessions;
    private readonly RTCConfiguration rtcConfiguration;
    private readonly IAudioSource? audioSource;
    private readonly IAudioSink? audioSink;
    private SipCredentials? registeredCredentials;

    // audioSource/audioSink are optional and platform-specific — this library has no built-in
    // microphone/speaker access of its own (SIPSorcery's own platform audio packages, e.g.
    // SIPSorceryMedia.Windows, need a platform-specific TFM this cross-platform project doesn't
    // have). Without them, calls still negotiate real SDP and connect (proven working), but no
    // actual audio flows — Asterisk's RTP-timeout hangs the call up after ~30s of silence
    // either way. Callers that want real two-way audio construct a platform audio endpoint
    // (e.g. SIPSorceryMedia.Windows's WindowsAudioEndPoint implements both interfaces) and
    // register it for DI; Microsoft.Extensions.DependencyInjection resolves these to null via
    // the default parameter when nothing's registered, so this stays a no-op on platforms
    // (e.g. Android) that don't provide one.
    public SipSorceryCallClient(
        IOptions<SipSorceryCallOptions> options,
        IAudioSource? audioSource = null,
        IAudioSink? audioSink = null)
    {
        this.audioSource = audioSource;
        this.audioSink = audioSink;
        this.options = options.Value;
        this.events = new Subject<CallClientEvent>();
        this.pendingIncomingCalls = [];
        this.activeMediaSessions = [];

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
                .ToList()
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

        this.userAgent = new SIPUserAgent(this.sipTransport, null);

        this.userAgent.OnIncomingCall += this.HandleIncomingCall;
        this.userAgent.OnDtmfTone += this.HandleDtmfTone;
        this.userAgent.OnCallHungup += this.HandleCallHungup;
        this.userAgent.ClientCallAnswered += this.HandleClientCallAnswered;
        this.userAgent.ClientCallFailed += this.HandleClientCallFailed;
    }

    public ValueTask RegisterAsync(SipCredentials credentials)
    {
        var registrationCompletionSource = new TaskCompletionSource();

        var registrationAgent = new SIPRegistrationUserAgent(
            this.sipTransport,
            credentials.Extension,
            credentials.Password,
            this.options.RegistrarHost,
            this.options.RegistrationExpirySeconds);

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

    public async ValueTask<CallHandle> PlaceCallAsync(string targetExtension)
    {
        var mediaSession = this.CreateMediaSession();

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
        this.userAgent.Hangup();

        if (this.activeMediaSessions.Remove(handle.Id, out RTCPeerConnection? mediaSession))
        {
            mediaSession.Close("hangup");
        }

        return ValueTask.CompletedTask;
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

    public IObservable<CallClientEvent> StreamEvents() => this.events;

    public void Dispose()
    {
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

    private RTCPeerConnection CreateMediaSession()
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

        if (this.audioSource is not null)
        {
            this.audioSource.OnAudioSourceEncodedSample += mediaSession.SendAudio;

            mediaSession.OnAudioFormatsNegotiated += negotiatedFormats =>
                this.audioSource.SetAudioSourceFormat(negotiatedFormats.First());

            mediaSession.onconnectionstatechange += async state =>
            {
                if (state == RTCPeerConnectionState.connected)
                {
                    await this.audioSource.StartAudio();
                }
                else if (state is RTCPeerConnectionState.closed
                    or RTCPeerConnectionState.failed
                    or RTCPeerConnectionState.disconnected)
                {
                    await this.audioSource.CloseAudio();
                }
            };
        }

        if (this.audioSink is not null)
        {
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
        }

        return mediaSession;
    }

    private void HandleIncomingCall(SIPUserAgent ua, SIPRequest sipRequest)
    {
        SIPServerUserAgent uas = ua.AcceptCall(sipRequest);
        var handle = new CallHandle { Id = Guid.NewGuid().ToString() };
        this.pendingIncomingCalls[handle.Id] = uas;

        this.PublishEvent("IncomingCall", handle, sipRequest.Header.From.FromURI.User);
    }

    private void HandleDtmfTone(byte tone, int durationMs) =>
        this.PublishEvent($"Dtmf:{tone}", new CallHandle());

    private void HandleCallHungup(SIPDialogue dialogue) =>
        this.PublishEvent("CallHungup", new CallHandle());

    private void HandleClientCallAnswered(ISIPClientUserAgent uac, SIPResponse sipResponse) =>
        this.PublishEvent("CallAnswered", new CallHandle());

    private void HandleClientCallFailed(ISIPClientUserAgent uac, string errorMessage, SIPResponse sipResponse) =>
        this.PublishEvent("CallFailed", new CallHandle());

    private void PublishEvent(string eventType, CallHandle handle, string remoteExtension = "") =>
        this.events.OnNext(new CallClientEvent
        {
            EventType = eventType,
            Handle = handle,
            OccurredDate = DateTimeOffset.UtcNow,
            RemoteExtension = remoteExtension
        });
}
