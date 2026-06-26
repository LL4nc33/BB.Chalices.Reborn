using Avalonia;
using ReactiveUI.Avalonia;

namespace BB.Chalices.App;

internal static class Program
{
    // Keep this free of any Avalonia/third-party calls until AppMain runs.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Also used by the previewer/designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI(_ => { });
}
