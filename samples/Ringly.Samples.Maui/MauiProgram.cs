using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Points at the local Docker Asterisk/coturn stack's own dev credentials
        // (docker/README.md) — edit these to match your own deployment.
        builder.Services.Configure<SipSorceryCallOptions>(options =>
        {
            // Asterisk's WSS signaling port, per docker/README.md — the SIP URI "transport=wss"
            // parameter tells SIPSorcery's registered SIPClientWebSocketChannel to use WSS.
            // "localhost" only works when Asterisk runs on the same machine as this app (e.g.
            // the Windows build against the local docker stack); an Android emulator needs
            // "10.0.2.2" instead of "localhost" to reach the host machine, and a real device
            // needs your machine's LAN IP.
            options.RegistrarHost = "localhost:8089;transport=wss";
            options.RegistrationExpirySeconds = 120;
            options.IceServerUrls = ["turn:localhost:3478"];
            options.IceServerUsername = "ringly";
            options.IceServerCredential = "ringly-dev-turn";
        });

        builder.Services.AddSingleton<ICallClient, SipSorceryCallClient>();
        builder.Services.AddSingleton<CallPage>();

        MauiApp app = builder.Build();

        // SipSorceryCallClient reads IOptions<SipSorceryCallOptions> in its constructor;
        // resolving it eagerly here (rather than lazily on first page navigation) means any
        // registration failure surfaces at startup, not mid-call.
        _ = app.Services.GetRequiredService<IOptions<SipSorceryCallOptions>>();

        return app;
    }
}
