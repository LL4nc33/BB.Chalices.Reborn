using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BB.Chalices.Data;
using BB.Chalices.Services;
using BB.Chalices.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BB.Chalices.App;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            desktop.MainWindow = window;
            _ = StartUpAsync(window);
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Create the database, seed it on first run, then hand the window its view model.
    private async Task StartUpAsync(Window window)
    {
        try
        {
            var db = Services!.GetRequiredService<ChaliceDbContext>();
            await db.Database.EnsureCreatedAsync();

            if (!db.Dungeons.Any())
            {
                var json = Path.Combine(AppContext.BaseDirectory, "dungeons.json");
                if (File.Exists(json))
                    await DungeonSeeder.SeedFromJsonAsync(db, json);
            }

            var viewModel = Services!.GetRequiredService<MainViewModel>();
            window.DataContext = viewModel;
            await viewModel.LoadDungeonsCommand.Execute();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Startup failed: {ex}");
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BBChalices", "chalices.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<ChaliceDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<ConfigService>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<SaveFileService>();
        services.AddSingleton<SaveLocatorService>();
        services.AddTransient<DungeonService>();
        services.AddTransient<MainViewModel>();
    }
}
