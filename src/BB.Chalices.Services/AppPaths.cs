namespace BB.Chalices.Services;

// Decides where the app keeps its data. Portable mode: if a "portable.txt" marker
// sits next to the executable, everything (settings, database, catalogue cache,
// backups) lives in a "data" folder beside the exe instead of in the user profile.
// Switching modes migrates the data on the next launch, before anything opens it.
public static class AppPaths
{
    private const string PortableMarker = "portable.txt";
    private const string FolderName = "BBChalices";

    private static readonly string ExeDir = AppContext.BaseDirectory;
    private static string MarkerPath => Path.Combine(ExeDir, PortableMarker);
    private static string PortableDir => Path.Combine(ExeDir, "data");
    private static string ProfileDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName);

    public static bool IsPortable { get; private set; }

    // The base folder all app data goes under. Resolved once at startup.
    public static string BaseDirectory { get; } = Resolve();

    private static string Resolve()
    {
        string active, inactive;
        try
        {
            if (File.Exists(MarkerPath))
            {
                IsPortable = true;
                active = PortableDir;
                inactive = ProfileDir;
            }
            else
            {
                active = ProfileDir;
                inactive = PortableDir;
            }
        }
        catch
        {
            active = ProfileDir;
            inactive = PortableDir;
        }

        Directory.CreateDirectory(active);

        // First launch in this mode with data still in the other location: bring it
        // over. Runs before the DB/settings are opened, so nothing is locked.
        try
        {
            if (!File.Exists(Path.Combine(active, "settings.json"))
                && Directory.Exists(inactive)
                && File.Exists(Path.Combine(inactive, "settings.json")))
            {
                CopyDirectory(inactive, active);
            }
        }
        catch
        {
            // Migration is best-effort; the app still runs with a fresh data folder.
        }

        return active;
    }

    // Turn portable mode on/off by creating/removing the marker. Takes effect on the
    // next launch, when Resolve migrates the data to the new location.
    public static void SetPortable(bool portable)
    {
        if (portable)
            File.WriteAllText(MarkerPath,
                "This file makes BB Chalices portable: its data is kept in the data/ folder next to the app.\n");
        else if (File.Exists(MarkerPath))
            File.Delete(MarkerPath);
    }

    // Portable toggling only makes sense where the exe folder is writable (not, say,
    // Program Files without elevation).
    public static bool CanTogglePortable()
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

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }
}
