using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Devices;
using Plugin.Maui.Audio;
using Shiny;
using Ringly.Client.Abstractions;
using Ringly.Client.SipSorcery;
using Ringly.Samples.BlazorHybrid.Brokers.Apis;
using Ringly.Samples.BlazorHybrid.ViewServices.Agents;
using Ringly.Samples.BlazorHybrid.ViewServices.Calls;
using Ringly.Samples.BlazorHybrid.ViewServices.Departments;
using Ringly.Samples.BlazorHybrid.ViewServices.Recordings;
using Ringly.Samples.BlazorHybrid.ViewServices.Support;
using Ringly.Samples.Maui;

namespace Ringly.Samples.BlazorHybrid;

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
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddTransient<MainPage>();
        builder.AddAudio();
        builder.UseShinyCamera();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

#if ANDROID
        string logDir = Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath ?? FileSystem.AppDataDirectory;
#else
        string logDir = FileSystem.AppDataDirectory;
#endif
        builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(logDir, "ringly-debug.log")));
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        // Same dev-stack/network configuration as Ringly.Samples.Maui's MauiProgram.cs — see that
        // file's own extensive comments for why each of these is needed (host resolution differs
        // by platform/emulator-vs-device, ICE/TURN bind-address quirks, etc.). Kept as a separate
        // copy (not shared) since each sample app is its own MAUI process with its own DI
        // container; edit both if your dev stack's address changes.
        // ringly:lan-ip
        const string CurrentLanHostAddress = "192.168.1.92";

        string host = DeviceInfo.Platform == DevicePlatform.Android
            ? (DeviceInfo.Current.DeviceType == DeviceType.Virtual ? "10.0.2.2" : CurrentLanHostAddress)
            : "localhost";

        builder.Services.Configure<SipSorceryCallOptions>(options =>
        {
            options.RegistrarHost = $"{host}:5060";
            options.RegistrationExpirySeconds = 120;

            string iceServerHost = DeviceInfo.Platform == DevicePlatform.Android ? host : CurrentLanHostAddress;
            options.IceServerUrls = [$"turn:{iceServerHost}:3478"];
            options.IceServerUsername = "ringly";
            options.IceServerCredential = "ringly-dev-turn";

#if WINDOWS
            options.BindAddress = CurrentLanHostAddress;
#elif ANDROID
            string? androidWifiAddress = GetAndroidWifiIPv4Address();

            if (androidWifiAddress is not null)
            {
                options.BindAddress = androidWifiAddress;
            }
#endif
        });

        // Ringly.Samples.WebApi's own default dev port (Properties/launchSettings.json) — same
        // host-resolution rules as the SIP registrar above (this app and the WebApi are assumed
        // to run on the same dev machine/network).
        builder.Services.Configure<SupportApiOptions>(options =>
            options.BaseUrl = $"http://{host}:5000");

        builder.Services.AddSingleton<ISupportApiBroker, SupportApiBroker>();
        builder.Services.AddSingleton<ISupportViewService, SupportViewService>();

        builder.Services.AddSingleton<IQueueApiBroker, QueueApiBroker>();
        builder.Services.AddSingleton<IDepartmentsViewService, DepartmentsViewService>();

        builder.Services.AddSingleton<IAgentConsoleApiBroker, AgentConsoleApiBroker>();
        builder.Services.AddSingleton<IAgentConsoleViewService, AgentConsoleViewService>();

        builder.Services.AddSingleton<IRecordingApiBroker, RecordingApiBroker>();
        builder.Services.AddSingleton<IRecordingViewService, RecordingViewService>();

#if WINDOWS
        // Real microphone/speaker access via the MAUI sample's CustomWindowsAudioEndPoint (linked
        // into this project — see the csproj's own comment).
        var audioEncoder = new SIPSorcery.Media.AudioEncoder();
        var windowsAudioEndPoint = new Ringly.Samples.Maui.Platforms.Windows.CustomWindowsAudioEndPoint(audioEncoder);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IAudioSource>(windowsAudioEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IAudioSink>(windowsAudioEndPoint);

        // Video counterpart — needs a native CameraView attached (see MainPage.xaml/.xaml.cs),
        // which is why this is registered as its own concrete singleton (not just under the
        // IVideoSource/IVideoSink abstractions) — MainPage resolves it directly via DI to call
        // AttachCameraView, the same way Ringly.Samples.Maui's CallPage does.
        var windowsVideoEndPoint = new Ringly.Samples.Maui.Platforms.Windows.CustomWindowsVideoEndPoint();
        builder.Services.AddSingleton(windowsVideoEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IVideoSource>(windowsVideoEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IVideoSink>(windowsVideoEndPoint);
        builder.Services.AddSingleton<IVideoFramePreviewSource, WindowsVideoFramePreviewSource>();
#elif ANDROID
        var androidAudioEncoder = new SIPSorcery.Media.AudioEncoder();
        var androidAudioEndPoint = new Ringly.Samples.Maui.Platforms.Android.AndroidAudioEndPoint(androidAudioEncoder);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IAudioSource>(androidAudioEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IAudioSink>(androidAudioEndPoint);

        // Headless CameraX capture — see AndroidCameraXVideoEndPoint's own class comment and
        // MainPage.xaml's comment for why Android doesn't need a native CameraView attached the
        // way Windows does.
        var androidVideoEndPoint = new Ringly.Samples.Maui.Platforms.Android.AndroidCameraXVideoEndPoint();
        builder.Services.AddSingleton(androidVideoEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IVideoSource>(androidVideoEndPoint);
        builder.Services.AddSingleton<SIPSorceryMedia.Abstractions.IVideoSink>(androidVideoEndPoint);
        builder.Services.AddSingleton<Ringly.Samples.Maui.Platforms.Android.IAndroidVideoCaptureEndPoint>(androidVideoEndPoint);
        builder.Services.AddSingleton<IVideoFramePreviewSource, AndroidVideoFramePreviewSource>();
#endif

        builder.Services.AddSingleton<ICallClient, SipSorceryCallClient>();

        // The single dependency CallScreen (the Core Component) integrates with — see
        // ICallViewService.cs's own comment.
        builder.Services.AddSingleton<ICallViewService, CallViewService>();

        MauiApp app = builder.Build();

        SIPSorcery.LogFactory.Set(app.Services.GetRequiredService<ILoggerFactory>());

        // SipSorceryCallClient reads IOptions<SipSorceryCallOptions> in its constructor;
        // resolving it eagerly here means any registration failure surfaces at startup, not
        // mid-call.
        _ = app.Services.GetRequiredService<IOptions<SipSorceryCallOptions>>();

        return app;
    }

#if ANDROID
    // See Ringly.Samples.Maui's MauiProgram.cs for the full explanation of why this reads the
    // device's Wi-Fi IPv4 address this way (deprecated-but-functional API, broad catch for a
    // startup-time best-effort read with a safe null fallback).
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
