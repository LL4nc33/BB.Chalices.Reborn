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
}
