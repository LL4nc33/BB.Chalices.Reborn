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
            const int catalogueVersion = 3;
            var config = Services!.GetRequiredService<ConfigService>();
            var dbFactory = Services!.GetRequiredService<IDbContextFactory<ChaliceDbContext>>();
            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();

                bool seeded = await db.Dungeons.AnyAsync();
                if (!seeded || config.Settings.CatalogueVersion != catalogueVersion)
                {
                    // The catalogue is Noxde's work, so it is not bundled. Seed only from
                    // the local copy the user consented to download (cached by ConfigService).
                    var localJson = config.CatalogueCachePath;
                    if (File.Exists(localJson))
                    {
                        await DungeonSeeder.ImportAsync(db, await File.ReadAllTextAsync(localJson), replaceExisting: true);
                        config.Settings.CatalogueVersion = catalogueVersion;
                        config.Save();
                    }
                }
            }

            var viewModel = Services!.GetRequiredService<MainViewModel>();
            window.DataContext = viewModel;
            await viewModel.LoadDungeonsCommand.Execute();

            // Always fill the character dropdown so nobody has to hunt for Detect,
            // then reopen the last save if it's still there.
            await viewModel.DetectSavesCommand.Execute();

            var lastSave = config.Settings.LastSavePath;
            if (!string.IsNullOrEmpty(lastSave) && File.Exists(lastSave))
                await viewModel.LoadSaveCommand.Execute(lastSave);
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
