using Ringly.Client.Abstractions;
using Ringly.Client.SipSorcery;
using Ringly.Samples.BlazorServer.Brokers.Audios;
using Ringly.Samples.BlazorServer.Components;
using Ringly.Samples.BlazorServer.ViewServices.Calls;

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
builder.Services.AddSingleton<ICallClient, SipSorceryCallClient>();

// The single dependency CallScreen (the Core Component) integrates with — see
// ICallViewService.cs's own comment.
builder.Services.AddSingleton<ICallViewService, CallViewService>();

var app = builder.Build();

SIPSorcery.LogFactory.Set(app.Services.GetRequiredService<ILoggerFactory>());

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
