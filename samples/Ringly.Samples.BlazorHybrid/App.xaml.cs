using Microsoft.Extensions.DependencyInjection;

namespace Ringly.Samples.BlazorHybrid;

public partial class App : Application
{
    private readonly IServiceProvider serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        this.serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Resolved via DI (not `new MainPage()`) so its Windows-only constructor parameter
        // (CustomWindowsVideoEndPoint, needed to attach the native local-camera-preview overlay —
        // see MainPage.xaml's own comment) can be satisfied the same way CallPage's is in
        // Ringly.Samples.Maui.
        var mainPage = this.serviceProvider.GetRequiredService<MainPage>();
        return new Window(mainPage) { Title = "Ringly.Samples.BlazorHybrid" };
    }
}
