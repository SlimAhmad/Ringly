using Ringly.Client.Abstractions;
using Ringly.Client.SipSorcery;
using Ringly.Samples.BlazorServer.Brokers.Apis;
using Ringly.Samples.BlazorServer.Brokers.Audios;
using Ringly.Samples.BlazorServer.Components;
using Ringly.Samples.BlazorServer.Video;
using Ringly.Samples.BlazorServer.ViewServices.Agents;
using Ringly.Samples.BlazorServer.ViewServices.Calls;
using Ringly.Samples.BlazorServer.ViewServices.Departments;
using Ringly.Samples.BlazorServer.ViewServices.Recordings;
using Ringly.Samples.BlazorServer.ViewServices.Support;
using SIPSorceryMedia.Windows;
using Vpx.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

// This sample runs as a normal server process — like Ringly.Samples.WebApi, it can reference
// Ringly.Client.SipSorcery directly and use real UDP sockets/local audio hardware, sidestepping
// the browser-sandbox problem a true Blazor WebAssembly client would hit (see the plan's own
// note on why WASM is explicitly out of scope). SipCall config lives in appsettings.json's
// "SipSorceryCall" section rather than hardcoded, since — unlike Ringly.Samples.Maui's
// MauiProgram.cs — there's no Android-emulator-vs-device host-resolution branching needed here,
// just one fixed server-side network configuration.
builder.Services.Configure<SipSorceryCallOptions>(
    builder.Configuration.GetSection("SipSorceryCall"));

// Real microphone/speaker access via the MAUI sample's CustomWindowsAudioEndPoint (linked into
// this project — see the csproj's own comment). Windows-only, matching this class's own NAudio
// WASAPI dependency — this sample assumes it runs on a Windows host.
var audioEncoder = new SIPSorcery.Media.AudioEncoder();
var windowsAudioEndPoint = new Ringly.Samples.Maui.Platforms.Windows.CustomWindowsAudioEndPoint(audioEncoder);
builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IAudioSource>(windowsAudioEndPoint);
builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IAudioSink>(windowsAudioEndPoint);

builder.Services.AddSingleton<IAudioTonePlayerBroker, AudioTonePlayerBroker>();

// Headless webcam capture — no CameraView/MAUI needed (see WindowsVideoFramePreviewSource.cs's
// own comment for why this replaced CustomWindowsVideoEndPoint's approach). Registered as its
// own concrete singleton (not just under IVideoSource/IVideoSink) so it can also be resolved
// directly for InitialiseVideoSourceDevice() below and by WindowsVideoFramePreviewSource.
var windowsVideoEndPoint = new WindowsVideoEndPoint(new VP8Codec());
builder.Services.AddSingleton(windowsVideoEndPoint);
builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IVideoSource>(windowsVideoEndPoint);
builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IVideoSink>(windowsVideoEndPoint);
builder.Services.AddSingleton<IVideoFramePreviewSource, WindowsVideoFramePreviewSource>();

builder.Services.AddSingleton<ICallClient, SipSorceryCallClient>();

// The single dependency CallScreen (the Core Component) integrates with — see
// ICallViewService.cs's own comment.
builder.Services.AddSingleton<ICallViewService, CallViewService>();

// Part 7: cold-support/queue-routing flow, calling Ringly.Samples.WebApi's
// ClientsController/SupportController — see ISupportApiBroker.cs's own comment.
builder.Services.AddSingleton<ISupportApiBroker, SupportApiBroker>();
builder.Services.AddSingleton<ISupportViewService, SupportViewService>();

builder.Services.AddSingleton<IQueueApiBroker, QueueApiBroker>();
builder.Services.AddSingleton<IDepartmentsViewService, DepartmentsViewService>();

builder.Services.AddSingleton<IRecordingApiBroker, RecordingApiBroker>();

// Scoped, not Singleton like its siblings above — confirmed live that DI validation fails
// outright at startup otherwise ("Cannot consume scoped service 'IJSRuntime' from singleton"):
// RecordingViewService.PlayAsync needs IJSRuntime, which Blazor Server registers per-circuit
// (Scoped), so this can't be a Singleton like the other ViewServices here, which don't need it.
builder.Services.AddScoped<IRecordingViewService, RecordingViewService>();

// Part 8c: agent console — availability toggle + live broadcast list, calling
// Ringly.Samples.WebApi's AgentsController.
builder.Services.AddSingleton<IAgentConsoleApiBroker, AgentConsoleApiBroker>();
builder.Services.AddSingleton<IAgentConsoleViewService, AgentConsoleViewService>();

var app = builder.Build();

SIPSorcery.LogFactory.Set(app.Services.GetRequiredService<ILoggerFactory>());

// Opens the default webcam once at startup rather than per-call — WindowsVideoEndPoint.StartVideo
// only starts/stops an already-initialised capture session, it doesn't open the device itself.
// Trade-off: the webcam indicator light turns on as soon as this process starts, not only during
// an actual video call — acceptable for a getting-started sample, revisit if that's undesirable
// for a real deployment (e.g. defer this until the first PlaceVideoCallAsync/AnswerAsync call).
bool videoDeviceInitialised = await windowsVideoEndPoint.InitialiseVideoSourceDevice();

if (!videoDeviceInitialised)
{
    app.Logger.LogWarning("No webcam found — video calls will have no outgoing video.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
