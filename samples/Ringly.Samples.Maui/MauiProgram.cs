using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Devices;
using Ringly.Client.Abstractions;
using Ringly.Client.SipSorcery;

namespace Ringly.Samples.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Logging.AddDebug();
#if ANDROID
        string logDir = Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath ?? FileSystem.AppDataDirectory;
#else
        string logDir = FileSystem.AppDataDirectory;
#endif
        builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(logDir, "ringly-debug.log")));
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        // Points at the local Docker Asterisk/coturn stack's own dev credentials
        // (docker/README.md) — edit these to match your own deployment.
        //
        // "localhost" only resolves to the host machine itself when this app also runs on
        // that machine (the Windows build). The Android emulator is its own virtual network —
        // "10.0.2.2" is its documented, fixed alias for the host machine's loopback, confirmed
        // by directly testing from an emulator shell (`nc -z 10.0.2.2 8089` succeeds,
        // `nc -z localhost 8089` is refused — localhost inside the emulator is the emulator
        // itself, not the host). A real Android device is on neither of those networks — it
        // needs your machine's actual LAN IP instead (find it with `ipconfig`), and the docker
        // stack's published ports need to be reachable from your LAN (Windows Firewall and, if
        // applicable, your router/AP client-isolation settings can block this even when the
        // ports are correctly published), or tunneled (e.g. ngrok) if it's off-network entirely
        // — neither verified here, only the emulator case was.
        string host = DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";

        builder.Services.Configure<SipSorceryCallOptions>(options =>
        {
            // Plain UDP against Asterisk's transport-udp (port 5060), not ws/wss — confirmed by
            // testing against a real Android emulator that neither works: wss:8089 fails with
            // SSLHandshakeException/CertificateException (Asterisk's transport-wss uses the
            // self-signed dev cert docker/asterisk/Dockerfile generates, and Android's platform
            // TLS stack rightly rejects it), and ws:8088 fails with a 404 on the WebSocket
            // handshake — SIPSorcery's SIPClientWebSocketChannel always connects to the server's
            // root path "/" with no way to target a sub-path (confirmed in its source), while
            // Asterisk's WebSocket transport is hardcoded to "/ws" (res_http_websocket) — the two
            // can never agree on a path. Native clients don't need WS anyway (it exists so
            // browsers, which lack raw socket access, can carry SIP over HTTP) — plain UDP is
            // simpler and just works. Use ws/wss instead if a future backend requires SIP over
            // WebSocket (e.g. reaching Asterisk through a proxy that only permits HTTP(S)).
            options.RegistrarHost = $"{host}:5060";
            options.RegistrationExpirySeconds = 120;
            options.IceServerUrls = [$"turn:{host}:3478"];
            options.IceServerUsername = "ringly";
            options.IceServerCredential = "ringly-dev-turn";
        });

        builder.Services.AddSingleton<ICallClient, SipSorceryCallClient>();
        builder.Services.AddSingleton<CallPage>();

        MauiApp app = builder.Build();

        SIPSorcery.LogFactory.Set(app.Services.GetRequiredService<ILoggerFactory>());

        // SipSorceryCallClient reads IOptions<SipSorceryCallOptions> in its constructor;
        // resolving it eagerly here (rather than lazily on first page navigation) means any
        // registration failure surfaces at startup, not mid-call.
        _ = app.Services.GetRequiredService<IOptions<SipSorceryCallOptions>>();

        return app;
    }
}
