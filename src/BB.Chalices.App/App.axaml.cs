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
            const int catalogueVersion = 2;
            var config = Services!.GetRequiredService<ConfigService>();
            var dbFactory = Services!.GetRequiredService<IDbContextFactory<ChaliceDbContext>>();
            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();

                var json = Path.Combine(AppContext.BaseDirectory, "dungeons.json");
                bool seeded = await db.Dungeons.AnyAsync();
                if (File.Exists(json) && (!seeded || config.Settings.CatalogueVersion != catalogueVersion))
                {
                    await DungeonSeeder.ImportAsync(db, await File.ReadAllTextAsync(json), replaceExisting: true);
                    config.Settings.CatalogueVersion = catalogueVersion;
                    config.Save();
                }
            }

            var viewModel = Services!.GetRequiredService<MainViewModel>();
            window.DataContext = viewModel;
            await viewModel.LoadDungeonsCommand.Execute();

            // Reopen the last save if it's still there; otherwise pre-fill the
            // shadPS4 dropdown so a first-time user does not have to find Detect.
            var lastSave = config.Settings.LastSavePath;
            if (!string.IsNullOrEmpty(lastSave) && File.Exists(lastSave))
                await viewModel.LoadSaveCommand.Execute(lastSave);
            else
                await viewModel.DetectSavesCommand.Execute();
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

        services.AddDbContextFactory<ChaliceDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<ConfigService>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<SaveFileService>();
        services.AddSingleton<SaveLocatorService>();
        services.AddTransient<DungeonService>();
        services.AddTransient<OnlineImportService>();
        services.AddTransient<MainViewModel>();
    }
}
