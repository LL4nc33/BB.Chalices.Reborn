namespace BB.Chalices.Services;

public sealed record BackupInfo(string FilePath, string DisplayName, DateTime Date, string OriginalFile, string Notes);

// Timestamped, browsable backups of save files with a small ".meta" sidecar.
// Carried over from the original OidaNice Custom Chalices backup manager.
public class BackupService
{
    private readonly ConfigService _config;

    public BackupService(ConfigService config) => _config = config;

    public string BackupDirectory => _config.BackupDirectory;

    public IReadOnlyList<BackupInfo> GetAll()
    {
        EnsureDirectory();

        var backups = new List<BackupInfo>();
        foreach (var file in Directory.GetFiles(BackupDirectory, "*.bak").OrderByDescending(File.GetLastWriteTime))
        {
            var date = File.GetLastWriteTime(file);
            var original = Path.GetFileNameWithoutExtension(file);
            var notes = string.Empty;

            var meta = file + ".meta";
            if (File.Exists(meta))
            {
                var fields = File.ReadAllLines(meta)
                    .Select(line => line.Split('=', 2))
                    .Where(parts => parts.Length == 2)
                    .ToDictionary(parts => parts[0], parts => parts[1]);

                if (fields.TryGetValue("OriginalFile", out var of) && !string.IsNullOrEmpty(of))
                    original = Path.GetFileName(of);
                if (fields.TryGetValue("Notes", out var n))
                    notes = n;
                if (fields.TryGetValue("Date", out var d) && DateTime.TryParse(d, out var parsed))
                    date = parsed;
            }

            backups.Add(new BackupInfo(file, $"{original}  ·  {date:yyyy-MM-dd HH:mm:ss}", date, original, notes));
        }
        return backups;
    }

    public string Create(string savePath, string notes = "")
    {
        if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
            return "Save file not found.";

        try
        {
            EnsureDirectory();
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var baseName = $"{Path.GetFileName(savePath)}_{stamp}";
            // Two backups can land in the same millisecond (e.g. a restore backs up the
            // current state right before overwriting it, and some clocks are coarse).
            // Disambiguate instead of overwriting, so an earlier backup is never clobbered.
            var name = $"{baseName}.bak";
            var dest = Path.Combine(BackupDirectory, name);
            for (int i = 2; File.Exists(dest); i++)
            {
                name = $"{baseName}_{i}.bak";
                dest = Path.Combine(BackupDirectory, name);
            }

            File.Copy(savePath, dest, overwrite: false);
            File.WriteAllText(dest + ".meta", $"OriginalFile={savePath}\nDate={DateTime.Now}\nNotes={notes}");

            return $"Backup created: {name}";
        }
        catch (Exception ex)
        {
            return $"Backup failed: {ex.Message}";
        }
    }

    public string Restore(string savePath, BackupInfo backup)
    {
        if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
            return "Save file not found.";
        if (!File.Exists(backup.FilePath))
            return "Backup file not found.";

        try
        {
            // Always keep an escape hatch: back up the current state before overwriting it.
            Create(savePath, "before restore");
            File.Copy(backup.FilePath, savePath, overwrite: true);
            return "Backup restored. The previous state was backed up first.";
        }
        catch (Exception ex)
        {
            return $"Restore failed: {ex.Message}";
        }
    }

    public string Delete(BackupInfo backup)
    {
        try
        {
            if (File.Exists(backup.FilePath))
                File.Delete(backup.FilePath);
            if (File.Exists(backup.FilePath + ".meta"))
                File.Delete(backup.FilePath + ".meta");
            return "Backup deleted.";
        }
        catch (Exception ex)
        {
            return $"Delete failed: {ex.Message}";
        }
    }

    private void EnsureDirectory() => Directory.CreateDirectory(BackupDirectory);
}
