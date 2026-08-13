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
    private SipCredentials? registeredCredentials;

    public SipSorceryCallClient(IOptions<SipSorceryCallOptions> options)
    {
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
        // negotiate). PCMU/PCMA are Asterisk's two allowed codecs for these test endpoints
        // (docker/asterisk/seed-test-endpoint.sql's "allow" list also includes opus, but PCMU/
        // PCMA need no extra codec package here). This declares the codecs a real call needs;
        // it does not by itself wire up microphone capture or speaker playback — callers still
        // need to attach an audio source/sink (e.g. via SIPSorceryMedia.Abstractions platform
        // bindings) for two-way audio, only signaling/negotiation is guaranteed to work here.
        var audioTrack = new MediaStreamTrack(
            new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMU),
            MediaStreamStatusEnum.SendRecv);

        mediaSession.addTrack(audioTrack);

        return mediaSession;
    }

    private void HandleIncomingCall(SIPUserAgent ua, SIPRequest sipRequest)
    {
        SIPServerUserAgent uas = ua.AcceptCall(sipRequest);
        var handle = new CallHandle { Id = Guid.NewGuid().ToString() };
        this.pendingIncomingCalls[handle.Id] = uas;

        this.PublishEvent("IncomingCall", handle);
    }

    private void HandleDtmfTone(byte tone, int durationMs) =>
        this.PublishEvent($"Dtmf:{tone}", new CallHandle());

    private void HandleCallHungup(SIPDialogue dialogue) =>
        this.PublishEvent("CallHungup", new CallHandle());

    private void HandleClientCallAnswered(ISIPClientUserAgent uac, SIPResponse sipResponse) =>
        this.PublishEvent("CallAnswered", new CallHandle());

    private void HandleClientCallFailed(ISIPClientUserAgent uac, string errorMessage, SIPResponse sipResponse) =>
        this.PublishEvent("CallFailed", new CallHandle());

    private void PublishEvent(string eventType, CallHandle handle) =>
        this.events.OnNext(new CallClientEvent
        {
            EventType = eventType,
            Handle = handle,
            OccurredDate = DateTimeOffset.UtcNow
        });
}
