namespace BB.Chalices.Services;

public enum StorageMode { Profile, Portable, Custom }

// Decides where the app keeps its data (settings, database, catalogue cache, backups).
// Three modes, chosen by marker files next to the executable so the location can be
// known before any data is opened:
//   - Profile (default): <LocalAppData>/BBChalices
//   - Portable  (portable.txt present): a "data" folder next to the exe
//   - Custom    (datadir.txt present):  the folder path stored in that file
// Switching records the old location so the next launch migrates the data across
// before anything is opened (no file locks).
public static class AppPaths
{
    private const string PortableMarker = "portable.txt";
    private const string CustomMarker = "datadir.txt";
    private const string MigrateMarker = "migrate-from.txt";
    private const string FolderName = "BBChalices";

    private static readonly string ExeDir = AppContext.BaseDirectory;
    private static string PortableMarkerPath => Path.Combine(ExeDir, PortableMarker);
    private static string CustomMarkerPath => Path.Combine(ExeDir, CustomMarker);
    private static string MigrateMarkerPath => Path.Combine(ExeDir, MigrateMarker);
    private static string PortableDir => Path.Combine(ExeDir, "data");
    private static string ProfileDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName);

    public static StorageMode Mode { get; private set; } = StorageMode.Profile;
    public static bool IsPortable => Mode == StorageMode.Portable;
    public static bool IsCustom => Mode == StorageMode.Custom;

    // The base folder all app data goes under. Resolved once at startup.
    public static string BaseDirectory { get; } = Resolve();

    private static string Resolve()
    {
        string active = ProfileDir;
        try
        {
            if (File.Exists(CustomMarkerPath) && File.ReadAllText(CustomMarkerPath).Trim() is { Length: > 0 } custom)
            {
                Mode = StorageMode.Custom;
                active = custom;
            }
            else if (File.Exists(PortableMarkerPath))
            {
                Mode = StorageMode.Portable;
                active = PortableDir;
            }
        }
        catch
        {
            Mode = StorageMode.Profile;
            active = ProfileDir;
        }

        try
        {
            Directory.CreateDirectory(active);
        }
        catch
        {
            // The chosen folder isn't usable (e.g. a removed drive): fall back to the profile.
            Mode = StorageMode.Profile;
            active = ProfileDir;
            Directory.CreateDirectory(active);
        }

        MigrateIfPending(active);
        return active;
    }

    // Bring data over from the location we were switching away from, if the new one is
    // still empty. Runs before the DB/settings are opened, so nothing is locked.
    private static void MigrateIfPending(string active)
    {
        try
        {
            if (!File.Exists(MigrateMarkerPath))
                return;

            var from = File.ReadAllText(MigrateMarkerPath).Trim();
            if (from.Length > 0 && !PathsEqual(from, active)
                && Directory.Exists(from) && File.Exists(Path.Combine(from, "settings.json"))
                && !File.Exists(Path.Combine(active, "settings.json")))
            {
                CopyDirectory(from, active);
            }
            File.Delete(MigrateMarkerPath);
        }
        catch
        {
            // Migration is best-effort; the app still runs with a fresh data folder.
        }
    }

    // Whether the exe folder is writable (markers can't be dropped in, say, Program Files).
    public static bool CanChangeLocation()
    {
        try
        {
            var probe = Path.Combine(ExeDir, ".bbchalices-write-test");
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void UseProfile()
    {
        RememberCurrentLocation();
        SafeDelete(CustomMarkerPath);
        SafeDelete(PortableMarkerPath);
    }

    public static void UsePortable()
    {
        RememberCurrentLocation();
        SafeDelete(CustomMarkerPath);
        File.WriteAllText(PortableMarkerPath,
            "This file makes BB Chalices portable: its data is kept in the data/ folder next to the app.\n");
    }

    public static void UseCustomFolder(string path)
    {
        RememberCurrentLocation();
        SafeDelete(PortableMarkerPath);
        File.WriteAllText(CustomMarkerPath, path.Trim());
    }

    private static void RememberCurrentLocation()
    {
        try { File.WriteAllText(MigrateMarkerPath, BaseDirectory); } catch { }
    }

    private static void SafeDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)),
                      Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),
                      StringComparison.OrdinalIgnoreCase);

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }
}
