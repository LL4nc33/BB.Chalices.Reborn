namespace BB.Chalices.Services;

// The app keeps its data (settings, database, catalogue cache, backups) in a
// "data" folder right next to the executable, so the whole thing is portable:
// move or copy the app folder and its data comes along. If that folder can't be
// written (e.g. the app sits in a read-only location like Program Files), it
// falls back to the user profile so the app still runs.
public static class AppPaths
{
    private const string FolderName = "BBChalices";

    private static readonly string ExeDir = AppContext.BaseDirectory;
    private static string PortableDir => Path.Combine(ExeDir, "data");
    private static string ProfileDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName);

    // The base folder all app data goes under. Resolved once at startup.
    public static string BaseDirectory { get; } = Resolve();

    // True when data lives next to the app (the normal case), false when it fell
    // back to the user profile because the app folder wasn't writable.
    public static bool IsPortable { get; private set; }

    private static string Resolve()
    {
        try
        {
            Directory.CreateDirectory(PortableDir);
            IsPortable = true;
            MigrateFromProfile(PortableDir);
            return PortableDir;
        }
        catch
        {
            // The app folder isn't writable: keep data in the user profile instead.
            IsPortable = false;
            Directory.CreateDirectory(ProfileDir);
            return ProfileDir;
        }
    }

    // One-time move-in: if the portable folder is fresh but an earlier install
    // left data in the user profile, bring it across so nothing is lost on upgrade.
    private static void MigrateFromProfile(string portable)
    {
        try
        {
            if (File.Exists(Path.Combine(portable, "settings.json")))
                return;
            if (Directory.Exists(ProfileDir) && File.Exists(Path.Combine(ProfileDir, "settings.json")))
                CopyDirectory(ProfileDir, portable);
        }
        catch
        {
            // Best-effort; the app still runs with a fresh data folder.
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
