using System.Reactive.Subjects;
using Microsoft.Extensions.Options;
using Ringly.Client.Abstractions;
using Ringly.Client.Abstractions.Models;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;

namespace Ringly.Client.SipSorcery;

public class SipSorceryCallClient : ICallClient, IDisposable
{
    private readonly SipSorceryCallOptions options;
    private readonly SIPTransport sipTransport;
    private readonly SIPUserAgent userAgent;
    private readonly Subject<CallClientEvent> events;
    private readonly Dictionary<string, SIPServerUserAgent> pendingIncomingCalls;
    private readonly Dictionary<string, RTCPeerConnection> activeMediaSessions;

    public SipSorceryCallClient(IOptions<SipSorceryCallOptions> options)
    {
        this.options = options.Value;
        this.events = new Subject<CallClientEvent>();
        this.pendingIncomingCalls = [];
        this.activeMediaSessions = [];

        this.sipTransport = new SIPTransport();
        this.sipTransport.AddSIPChannel(new SIPClientWebSocketChannel());

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
            registrationCompletionSource.TrySetResult();

        registrationAgent.RegistrationFailed += (uri, response, errorMessage) =>
            registrationCompletionSource.TrySetException(new InvalidOperationException(errorMessage));

        registrationAgent.Start();

        return new ValueTask(registrationCompletionSource.Task);
    }

    public async ValueTask<CallHandle> PlaceCallAsync(string targetExtension)
    {
        var mediaSession = new RTCPeerConnection();

        bool callResult = await this.userAgent.Call(
            targetExtension,
            username: null,
            password: null,
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

        var mediaSession = new RTCPeerConnection();
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
