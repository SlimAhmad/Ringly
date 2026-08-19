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
        // IP instead. DeviceInfo.Current.DeviceType distinguishes an emulator (Virtual) from a
        // real device (Physical) so both can be tested without editing this by hand each time.
        // The docker stack's published ports need to be reachable from your LAN (Windows
        // Firewall and, if applicable, your router/AP client-isolation settings can block this
        // even when the ports are correctly published), or tunneled (e.g. ngrok) if it's
        // off-network entirely.
        //
        // Must be kept in sync with docker/coturn/turnserver.conf's external-ip — see that file's
        // comment for why (coturn advertises this as the relay candidate address, so a stale
        // value here/there causes silent mid-call ICE/TURN relay failures, not a loud
        // connection-time error). Run docker/update-network-ip.ps1 after switching networks to
        // update both this and turnserver.conf together and restart coturn in one step, instead
        // of editing by hand — it looks for the "ringly:lan-ip" marker below to find this line.
        // ringly:lan-ip
        const string CurrentLanHostAddress = "192.168.1.92";

        string host = DeviceInfo.Platform == DevicePlatform.Android
            ? (DeviceInfo.Current.DeviceType == DeviceType.Virtual ? "10.0.2.2" : CurrentLanHostAddress)
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

            // NOT $"turn:{host}:3478" — on Windows, host is "localhost" for SIP signaling
            // (registration/INVITE have always worked fine that way, unrelated to this), but the
            // ICE server URL specifically must NOT resolve to loopback here. Confirmed live: once
            // X_BindAddress (below) started binding the RTP socket to this machine's real LAN IP,
            // every single TURN allocate request to turn:localhost (127.0.0.1) started failing
            // immediately and unconditionally with SocketException 10049 ("The requested address
            // is not valid in its context") — Windows Sockets refuses to send loopback-destined
            // traffic from a socket explicitly bound to a real, non-loopback address. Using the
            // same LAN IP for both fixes it: sending from 10.x.x.x to 10.x.x.x:3478 is a normal
            // same-interface send, not a loopback/real-IP mismatch. Android already used its own
            // real address here (CurrentLanHostAddress is literally what it dials Asterisk/coturn
            // at), so this just brings Windows in line with the same reasoning.
            string iceServerHost = DeviceInfo.Platform == DevicePlatform.Android ? host : CurrentLanHostAddress;
            options.IceServerUrls = [$"turn:{iceServerHost}:3478"];
            options.IceServerUsername = "ringly";
            options.IceServerCredential = "ringly-dev-turn";

#if WINDOWS
            // Confirmed live: this machine's Docker Desktop WSL2 bridge adapter shares a subnet
            // with this project's own docker-compose network (172.22.0.0/16) purely by
            // coincidence — without binding explicitly, ICE gathered (and the remote peer
            // nominated) a host candidate on that virtual, Windows-host-only adapter instead of
            // the real Wi-Fi one, causing a DTLS handshake timeout with no error until then. See
            // SipSorceryCallOptions.BindAddress's own comment for the full story.
            options.BindAddress = CurrentLanHostAddress;
#elif ANDROID
            // Same class of bug as Windows' own X_BindAddress fix above, confirmed live via a
            // real Asterisk SIP/SDP trace (pjsip set logger on / rtp set debug on): Android's own
            // ICE candidate gathering picked up its CELLULAR/LTE interface instead of Wi-Fi
            // (offered "typ host" candidate address 10.104.150.211 — a mobile-carrier APN-style
            // address, not reachable from anything outside that carrier's own network) even
            // though the phone was simultaneously connected to Wi-Fi. Both radios stay active at
            // once on a real device, and RtpIceChannel enumerates every local interface, gathering
            // and offering a candidate for whichever one it finds — Asterisk's RTP debug trace
            // confirmed zero packets ever arrived from Android's leg as a direct result: media was
            // being sent to (and expected from) an address nothing outside the phone's own carrier
            // network could ever reach. Unlike Windows' host constant, there's no fixed IP to hard
            // code here — it has to be read from the device's own current Wi-Fi connection at
            // runtime. GetAndroidWifiIPv4Address() below does that; if it can't find one (e.g.
            // Wi-Fi genuinely isn't connected), BindAddress is left null and ICE falls back to its
            // original everything-interfaces behavior rather than binding to nothing at all.
            string? androidWifiAddress = GetAndroidWifiIPv4Address();

            if (androidWifiAddress is not null)
            {
                options.BindAddress = androidWifiAddress;
            }
#endif
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

        // Video counterpart — two implementations exist side by side for A/B testing (see #143 /
        // AndroidCameraXVideoEndPoint.cs's own comment for the full story): AndroidVideoEndPoint
        // drives capture through Shiny.Maui.Controls.Camera's CameraView/IFrameAnalyzer;
        // AndroidCameraXVideoEndPoint bypasses Shiny entirely and drives CameraX's
        // ProcessCameraProvider/ImageAnalysis directly. Flip this flag to switch which one is
        // registered — both implement IVideoSource/IVideoSink/IAndroidVideoCaptureEndPoint
        // identically, so nothing else (CallPage included) needs to change either way.
        const bool useHandRolledAndroidCamera = true;

        SIPSorceryMedia.Abstractions.IVideoSource androidVideoSource;
        SIPSorceryMedia.Abstractions.IVideoSink androidVideoSink;
        Ringly.Samples.Maui.Platforms.Android.IAndroidVideoCaptureEndPoint androidVideoCaptureEndPoint;

        if (useHandRolledAndroidCamera)
        {
            var handRolledVideoEndPoint = new Ringly.Samples.Maui.Platforms.Android.AndroidCameraXVideoEndPoint();
            androidVideoSource = handRolledVideoEndPoint;
            androidVideoSink = handRolledVideoEndPoint;
            androidVideoCaptureEndPoint = handRolledVideoEndPoint;
        }
        else
        {
            var shinyVideoEndPoint = new Ringly.Samples.Maui.Platforms.Android.AndroidVideoEndPoint();
            androidVideoSource = shinyVideoEndPoint;
            androidVideoSink = shinyVideoEndPoint;
            androidVideoCaptureEndPoint = shinyVideoEndPoint;
        }

        builder.Services.AddSingleton(androidVideoCaptureEndPoint);
        builder.Services.AddSingleton(androidVideoSource);
        builder.Services.AddSingleton(androidVideoSink);
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

#if ANDROID
    // WifiManager.ConnectionInfo.IpAddress is deprecated as of API 31 (Android recommends
    // ConnectivityManager/LinkProperties instead) but still functions — a fine tradeoff for a
    // getting-started sample versus the added complexity of the newer API. Returns the device's
    // current Wi-Fi IPv4 address, or null if Wi-Fi isn't connected (IpAddress reads 0 in that
    // case) or the call fails for any reason, so callers can fall back to leaving BindAddress
    // unset entirely rather than this taking down app startup. Confirmed live: without
    // ACCESS_WIFI_STATE declared in AndroidManifest.xml, WifiManager.ConnectionInfo throws
    // Java.Lang.SecurityException at startup — CreateMauiApp() runs this before the app has any
    // UI to show an error in, so an uncaught exception here is a silent full crash on launch, not
    // a graceful "no video/audio this call" degradation the way a missing camera/mic permission
    // is elsewhere in this file. Caught broadly (not just SecurityException) since this is a
    // best-effort read with a safe fallback already defined either way.
    private static string? GetAndroidWifiIPv4Address()
    {
        int rawAddress;

        try
        {
            using var wifiManager = global::Android.App.Application.Context
                .GetSystemService(global::Android.Content.Context.WifiService) as global::Android.Net.Wifi.WifiManager;

            rawAddress = wifiManager?.ConnectionInfo?.IpAddress ?? 0;
        }
        catch (Exception)
        {
            return null;
        }

        if (rawAddress == 0)
        {
            return null;
        }

        // IpAddress is a little-endian-encoded int32 regardless of the device's own byte order —
        // documented Android behavior, not something DeviceInfo/BitConverter.IsLittleEndian needs
        // to account for.
        var addressBytes = new byte[]
        {
            (byte)(rawAddress & 0xFF),
            (byte)((rawAddress >> 8) & 0xFF),
            (byte)((rawAddress >> 16) & 0xFF),
            (byte)((rawAddress >> 24) & 0xFF)
        };

        return new System.Net.IPAddress(addressBytes).ToString();
    }
#endif
}
