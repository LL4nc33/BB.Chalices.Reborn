namespace BB.Chalices.Services;

// User settings, persisted as JSON next to the database.
public class AppSettings
{
    // Where shadPS4 is unpacked (overrides auto-detection when set).
    public string? ShadPs4FolderPath { get; set; }

    // The shadPS4 program to launch (overrides auto-detection when set). Lets the
    // user point at the exact file when the build's name or location is unusual.
    public string? ShadPs4ExePath { get; set; }

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

    // Set once the old Custom-category dungeons have been migrated into a list, so a
    // later launch doesn't resurrect ones the user has since removed.
    public bool CustomMigrated { get; set; }

    // Per-column zoom factor (1.0 = 100% = the column's default size), adjustable with
    // the +/- buttons. The actual scale is this factor times the column's baseline.
    public double SidebarZoom { get; set; } = 1.0;
    public double CatalogueZoom { get; set; } = 1.0;
    public double EditorZoom { get; set; } = 1.0;
}
