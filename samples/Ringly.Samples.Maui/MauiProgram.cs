using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Devices;
using Plugin.Maui.Audio;
using Shiny;
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

        builder.AddAudio();
        builder.UseShinyCamera();

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
        // itself, not the host). A real Android device is on NEITHER of those networks — it's
        // just another device on the same LAN, and needs the host machine's actual routable LAN
        // IP instead (find it with `ipconfig` — this is also what coturn's
        // docker/coturn/turnserver.conf external-ip is set to, and needs updating alongside it if
        // this network changes). DeviceInfo.Current.DeviceType distinguishes an emulator
        // (Virtual) from a real device (Physical) so both can be tested without editing this by
        // hand each time. The docker stack's published ports need to be reachable from your LAN
        // (Windows Firewall and, if applicable, your router/AP client-isolation settings can
        // block this even when the ports are correctly published), or tunneled (e.g. ngrok) if
        // it's off-network entirely.
        // Previous value "10.253.155.49" was the phone's own mobile-hotspot Wi-Fi — confirmed a
        // likely source of the packet loss/jitter behind choppy audio and BYE timeouts (see
        // CustomWindowsAudioEndPoint.cs/AndroidAudioEndPoint.cs), since the phone was acting as
        // both the AP and a real-time audio endpoint at once. Kept here as
        // MobileWifiLanHostAddress for reference if reverting to that setup.
        // const string MobileWifiLanHostAddress = "10.253.155.49";

        // Current LAN: both devices on the same router-provided Wi-Fi (found via
        // Get-NetIPAddress/ipconfig on the "Wi-Fi" adapter, not Tailscale/VPN/APIPA addresses).
        const string LanHostAddress = "192.168.101.36";

        string host = DeviceInfo.Platform == DevicePlatform.Android
            ? (DeviceInfo.Current.DeviceType == DeviceType.Virtual ? "10.0.2.2" : LanHostAddress)
            : "localhost";

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

#if WINDOWS
        // Diagnostic: log every capture device NAudio's WaveInEvent (the legacy WinMM API
        // WindowsAudioEndPoint uses internally) actually sees, and which one index -1/default
        // resolves to. This machine has multiple audio devices registered (a Bluetooth-paired
        // phone as a hands-free device, an EShare virtual mic alongside the real Realtek
        // microphone array) — WinMM's own device ordering/"default" concept is a legacy API
        // that can disagree with what Windows Sound Settings shows as the default communications
        // device, especially with several devices in play. Written directly to the log file
        // (not via ILogger) since WindowsAudioEndPoint is constructed before the DI container
        // — and therefore the real ILoggerFactory — exists.
        try
        {
            string deviceLogPath = Path.Combine(logDir, "ringly-debug.log");
            var deviceLogLines = new List<string> { $"{DateTimeOffset.UtcNow:HH:mm:ss.fff} [Information] NAudio device enumeration: WaveInEvent.DeviceCount={NAudio.Wave.WaveInEvent.DeviceCount}" };

            for (int i = 0; i < NAudio.Wave.WaveInEvent.DeviceCount; i++)
            {
                var capabilities = NAudio.Wave.WaveInEvent.GetCapabilities(i);
                deviceLogLines.Add($"{DateTimeOffset.UtcNow:HH:mm:ss.fff} [Information]   WaveIn device {i}: \"{capabilities.ProductName}\" ({capabilities.Channels} channel(s))");
            }

            byte[] deviceLogBytes = System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, deviceLogLines) + Environment.NewLine);
            using var deviceLogStream = new FileStream(deviceLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            deviceLogStream.Write(deviceLogBytes, 0, deviceLogBytes.Length);
        }
        catch (Exception)
        {
        }

        // Real microphone/speaker access via CustomWindowsAudioEndPoint (see its own file for
        // why this hand-rolls NAudio's WASAPI capture directly instead of using
        // SIPSorceryMedia.Windows's WindowsAudioEndPoint, and why WASAPI specifically instead of
        // the legacy WinMM device-index approach this used earlier — that produced every step
        // reporting success, including with an explicit device pin, yet the other party
        // consistently received digital silence with no visibility into why). Registering the
        // one instance under both interfaces lets SipSorceryCallClient's optional constructor
        // parameters resolve it for both directions. Capture device selection is now handled
        // internally via the Communications-role default endpoint (see StartAudio()), not an
        // explicit index — the device enumeration logged above remains useful diagnostic context
        // even though it no longer drives an explicit index.
        var audioEncoder = new SIPSorcery.Media.AudioEncoder();
        var windowsAudioEndPoint = new Ringly.Samples.Maui.Platforms.Windows.CustomWindowsAudioEndPoint(audioEncoder);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IAudioSource>(windowsAudioEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IAudioSink>(windowsAudioEndPoint);

        // Video counterpart — see CustomWindowsVideoEndPoint.cs for why this hand-rolls a
        // Shiny.Maui.Controls.Camera IFrameAnalyzer + SIPSorcery.VP8 pipeline instead of using a
        // pre-built SIPSorceryMedia video package (none exists cross-platform). Registered as a
        // singleton the same way as audio; also registered under its own concrete type so
        // CallPage can resolve it directly to call AttachCameraView/DetachCameraView, which
        // aren't part of the IVideoSource/IVideoSink abstractions.
        var windowsVideoEndPoint = new Ringly.Samples.Maui.Platforms.Windows.CustomWindowsVideoEndPoint();
        builder.Services.AddSingleton(windowsVideoEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IVideoSource>(windowsVideoEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IVideoSink>(windowsVideoEndPoint);
#elif ANDROID
        // No official SIPSorceryMedia.Android package exists — AndroidAudioEndPoint hand-rolls
        // the same source/sink surface via AudioRecord/AudioTrack (see its own file for why).
        var androidAudioEncoder = new SIPSorcery.Media.AudioEncoder();
        var androidAudioEndPoint = new Ringly.Samples.Maui.Platforms.Android.AndroidAudioEndPoint(androidAudioEncoder);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IAudioSource>(androidAudioEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IAudioSink>(androidAudioEndPoint);

        // Video counterpart — see AndroidVideoEndPoint.cs for why this repacks CameraX's
        // YUV_420_888 planes into I420 instead of hand-rolling Camera2 directly. Registered the
        // same way as the Windows video endpoint: a singleton under both IVideoSource/IVideoSink
        // for SipSorceryCallClient's optional constructor parameters, plus its own concrete type
        // so CallPage can resolve it directly to call AttachCameraView/DetachCameraView.
        var androidVideoEndPoint = new Ringly.Samples.Maui.Platforms.Android.AndroidVideoEndPoint();
        builder.Services.AddSingleton(androidVideoEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IVideoSource>(androidVideoEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IVideoSink>(androidVideoEndPoint);
#endif

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
