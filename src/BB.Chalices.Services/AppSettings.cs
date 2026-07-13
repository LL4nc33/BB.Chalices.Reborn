namespace BB.Chalices.Services;

// User settings, persisted as JSON next to the database.
public class AppSettings
{
    // Where shadPS4 is unpacked (overrides auto-detection when set).
    public string? ShadPs4FolderPath { get; set; }

    // The save that was open last, for convenience.
    public string? LastSavePath { get; set; }

    // Keep a timestamped backup in the backup folder every time we save.
    public bool AutoBackupEnabled { get; set; } = true;

    // Where managed backups live (defaults to <LocalAppData>/BBChalices/Backups).
    public string? BackupDirectory { get; set; }

    // Remembered main-window size so it reopens the way you left it.
    public int? WindowWidth { get; set; }
    public int? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }

    // Bumped when the bundled catalogue changes, to trigger a one-time reseed.
    public int CatalogueVersion { get; set; }

    // UI zoom factor (1.0 = 100%), adjustable with the +/- buttons.
    public double UiScale { get; set; } = 1.0;
}
