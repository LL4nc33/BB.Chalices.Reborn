using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BB.Chalices.Data;
using BB.Chalices.Data.Entities;
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
            const int catalogueVersion = 4;
            var config = Services!.GetRequiredService<ConfigService>();
            var dbFactory = Services!.GetRequiredService<IDbContextFactory<ChaliceDbContext>>();
            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();

                bool seeded = await db.Dungeons.AnyAsync();
                bool reseeded = !seeded || config.Settings.CatalogueVersion != catalogueVersion;
                if (reseeded)
                {
                    // 1) Our bundled by-area catalogue (the "All" view) - always available,
                    //    works fully offline. Embedded in the executable.
                    await using (var bundled = typeof(App).Assembly.GetManifestResourceStream("catalogue.json"))
                    {
                        if (bundled is not null)
                        {
                            using var reader = new StreamReader(bundled);
                            await DungeonSeeder.ImportAsync(db, await reader.ReadToEndAsync(), replaceExisting: true);
                        }
                    }

                    // 2) Nox's curated categories, only if a consented copy was downloaded.
                    //    Upsert-by-glyph keeps the bundled set and any custom dungeons.
                    if (File.Exists(config.CatalogueCachePath))
                        await DungeonSeeder.ImportAsync(db, await File.ReadAllTextAsync(config.CatalogueCachePath), replaceExisting: true);

                    config.Settings.CatalogueVersion = catalogueVersion;
                    config.Save();
                }

                // Lists live in the same database. Always make sure their tables exist,
                // but only rebuild the built-in lists when the catalogue actually changed
                // (or they don't exist yet) - rebuilding churns thousands of rows.
                await ListBootstrapper.EnsureSchemaAsync(db);
                if (reseeded || !await db.Lists.AnyAsync(l => l.Source != ListSource.User))
                    await ListBootstrapper.RebuildBuiltInListsAsync(db);

                // Migrate old custom dungeons into a list once, so a removed one isn't
                // resurrected on the next launch.
                if (!config.Settings.CustomMigrated)
                {
                    await ListBootstrapper.MigrateCustomAsync(db);
                    config.Settings.CustomMigrated = true;
                    config.Save();
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
        var dbPath = Path.Combine(AppPaths.BaseDirectory, "chalices.db");

        services.AddDbContextFactory<ChaliceDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<ConfigService>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<SaveFileService>();
        services.AddSingleton<SaveLocatorService>();
        services.AddTransient<DungeonService>();
        services.AddTransient<ListService>();
        services.AddTransient<OnlineImportService>();
        services.AddTransient<MainViewModel>();
    }
}
