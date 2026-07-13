namespace BB.Chalices.Services;

// Decides where the app keeps its data. Portable mode: if a "portable.txt" marker
// sits next to the executable, everything (settings, database, catalogue cache)
// lives in a "data" folder beside the exe instead of in the user profile.
public static class AppPaths
{
    private const string PortableMarker = "portable.txt";
    private const string FolderName = "BBChalices";

    // The base folder all app data goes under. Resolved once at startup.
    public static string BaseDirectory { get; } = Resolve();

    public static bool IsPortable { get; private set; }

    private static string Resolve()
    {
        try
        {
            var exeDir = AppContext.BaseDirectory;
            if (File.Exists(Path.Combine(exeDir, PortableMarker)))
            {
                IsPortable = true;
                var portable = Path.Combine(exeDir, "data");
                Directory.CreateDirectory(portable);
                return portable;
            }
        }
        catch
        {
            // If the exe folder is unreadable/unwritable, fall back to the profile.
        }

        var profile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName);
        Directory.CreateDirectory(profile);
        return profile;
    }
}
