using System.Text.Json;

namespace BB.Chalices.Services;

// Loads and saves user settings in <LocalAppData>/BBChalices/settings.json.
public class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _appDir;
    private readonly string _path;

    public ConfigService()
    {
        _appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BBChalices");
        Directory.CreateDirectory(_appDir);
        _path = Path.Combine(_appDir, "settings.json");
        Settings = Load();
    }

    public AppSettings Settings { get; private set; }

    public string BackupDirectory =>
        string.IsNullOrWhiteSpace(Settings.BackupDirectory)
            ? Path.Combine(_appDir, "Backups")
            : Settings.BackupDirectory!;

    public void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(Settings, JsonOptions));
        }
        catch
        {
            // Settings are best-effort; never let a write failure crash the app.
        }
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
        }
        catch
        {
            // Corrupt or unreadable settings fall back to defaults.
        }
        return new AppSettings();
    }
}
