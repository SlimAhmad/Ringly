namespace Ringly.Samples.BlazorHybrid;

public partial class MainPage : ContentPage
{
#if WINDOWS
    private readonly Ringly.Samples.Maui.Platforms.Windows.CustomWindowsVideoEndPoint? windowsVideoEndPoint;

    public MainPage(Ringly.Samples.Maui.Platforms.Windows.CustomWindowsVideoEndPoint windowsVideoEndPoint)
    {
        InitializeComponent();
        this.windowsVideoEndPoint = windowsVideoEndPoint;
    }
#else
    public MainPage()
    {
        InitializeComponent();
    }
#endif

    protected override void OnAppearing()
    {
        base.OnAppearing();

#if WINDOWS
        this.windowsVideoEndPoint?.AttachCameraView(this.LocalCameraView);
#endif
    }
}
