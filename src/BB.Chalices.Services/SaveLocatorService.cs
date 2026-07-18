using System.Diagnostics;

namespace BB.Chalices.Services;

// Finds Bloodborne saves written by the shadPS4 emulator. The layout is
//   <shadPS4>/user/home/<id>/savedata/<CUSAxxxxx>/SPRJ0005/userdataNNNN
// but the account id and the CUSA title id depend on the build and the game
// version, so instead of hard-coding the path we just look for any SPRJ0005
// folder (Bloodborne's save id) under a configured root. shadPS4 stores saves
// decrypted, so the files underneath can be edited in place.
public class SaveLocatorService
{
    public const string BloodborneSaveFolder = "SPRJ0005";

    // Character saves are userdata0000, userdata0001, ... The system/options
    // file (userdata...10) and shadPS4's *.bak backups are not characters.
    public static bool IsCharacterSave(string fileName) =>
        fileName.StartsWith("userdata", StringComparison.OrdinalIgnoreCase)
        && !fileName.Contains(".bak", StringComparison.OrdinalIgnoreCase)
        && !fileName.EndsWith("10", StringComparison.OrdinalIgnoreCase);

    // Every SPRJ0005 folder under the given shadPS4 root (one per title id).
    public IReadOnlyList<string> FindSaveFolders(string shadPs4Root)
    {
        if (string.IsNullOrWhiteSpace(shadPs4Root) || !Directory.Exists(shadPs4Root))
            return Array.Empty<string>();

        // Newer builds use user/home/<id>/savedata, older ones user/savedata/<id>;
        // start at user/ when present to keep the walk small.
        var start = Directory.Exists(Path.Combine(shadPs4Root, "user"))
            ? Path.Combine(shadPs4Root, "user")
            : shadPs4Root;

        return FindFoldersNamed(start, BloodborneSaveFolder)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> FindSaveFiles(string saveFolder)
    {
        if (!Directory.Exists(saveFolder))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(saveFolder)
            .Where(p => IsCharacterSave(Path.GetFileName(p)))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // shadPS4 sound-crash workaround: the options file (userdata0010) has its byte
    // at offset 0x204E flipped from 00 to 01. (Nexus mod 165, credit proteh.)
    public const int SoundFixOffset = 0x204E;

    // The options file (userdata...10) in a save folder, if it is there.
    public string? FindSystemFile(string saveFolder)
    {
        if (!Directory.Exists(saveFolder))
            return null;

        return Directory.EnumerateFiles(saveFolder).FirstOrDefault(p =>
        {
            string name = Path.GetFileName(p);
            return name.StartsWith("userdata", StringComparison.OrdinalIgnoreCase)
                && !name.Contains(".bak", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith("10", StringComparison.OrdinalIgnoreCase);
        });
    }

    public bool IsSoundFixApplied(string systemFile)
    {
        try
        {
            using var stream = File.OpenRead(systemFile);
            if (stream.Length <= SoundFixOffset)
                return false;
            stream.Seek(SoundFixOffset, SeekOrigin.Begin);
            return stream.ReadByte() == 0x01;
        }
        catch
        {
            return false;
        }
    }

    public void ApplySoundFix(string systemFile)
    {
        byte[] bytes = File.ReadAllBytes(systemFile);
        if (bytes.Length <= SoundFixOffset)
            return;
        bytes[SoundFixOffset] = 0x01;
        File.WriteAllBytes(systemFile, bytes);
    }

    // The usual places shadPS4 ends up, across Windows, Linux and macOS.
    // Returns null if none look right (the user can still browse manually).
    public string? GuessShadPs4Root()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);      // Windows %AppData%, Unix ~/.config
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);   // Windows %LocalAppData%, Unix ~/.local/share

        string[] candidates =
        [
            // Portable installs people keep around (all platforms)
            Path.Combine(home, "shadPS4"),
            Path.Combine(home, "Downloads", "shadPS4"),
            Path.Combine(home, "Desktop", "shadPS4"),
            Path.Combine(home, "Documents", "shadPS4"),
            // Per-OS app-data locations
            Path.Combine(roaming, "shadPS4"),
            Path.Combine(local, "shadPS4"),
            Path.Combine(home, "Library", "Application Support", "shadPS4"),          // macOS
            Path.Combine(home, ".var", "app", "net.shadps4.shadPS4", "data", "shadPS4"), // Linux (Flatpak)
        ];

        return Array.Find(candidates, c => Directory.Exists(Path.Combine(c, "user")));
    }

    // Passed to Launch() when the program is a Flatpak app rather than a file.
    private const string FlatpakPrefix = "flatpak:";
    private const string FlatpakAppId = "net.shadps4.shadPS4";

    // Locates the shadPS4 program so it can be launched, or null if not found.
    // Windows portable installs keep the program next to the user/ folder, but on
    // macOS (Application Support) and Linux Flatpak the save data lives apart from
    // the program, so we also check the usual install locations and the PATH, and
    // fall back to launching the Flatpak app.
    public string? FindProgram(string root)
    {
        // Flatpak keeps saves under ~/.var/app/<id>/...; launch via `flatpak run`.
        if (OperatingSystem.IsLinux() && root.Replace('\\', '/').Contains("/.var/app/" + FlatpakAppId))
            return FlatpakPrefix + FlatpakAppId;

        foreach (var dir in new[] { root, Directory.GetParent(root)?.FullName })
        {
            if (dir is null || !Directory.Exists(dir))
                continue;
            string? hit = FindProgramIn(dir);
            if (hit is not null)
                return hit;
        }

        // Install locations the save folder doesn't cover (macOS .app, Linux bins).
        foreach (string dir in InstallLocations())
            if (Directory.Exists(dir) && FindProgramIn(dir) is { } hit)
                return hit;

        return OnPath();
    }

    // Common places the program is installed, separate from the save data.
    private static IEnumerable<string> InstallLocations()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (OperatingSystem.IsMacOS())
        {
            yield return "/Applications";
            yield return Path.Combine(home, "Applications");
        }
        else if (OperatingSystem.IsLinux())
        {
            yield return "/usr/bin";
            yield return "/usr/local/bin";
            yield return Path.Combine(home, ".local", "bin");
            yield return Path.Combine(home, "Applications");
            yield return Path.Combine(home, "Downloads");
        }
    }

    // A shadPS4 binary on the PATH (Linux/macOS command-line installs).
    private static string? OnPath()
    {
        if (OperatingSystem.IsWindows())
            return null;
        string[] dirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(':', StringSplitOptions.RemoveEmptyEntries);
        foreach (string dir in dirs)
            foreach (string name in new[] { "shadps4", "Shadps4-qt", "shadPS4" })
            {
                string path = Path.Combine(dir, name);
                if (File.Exists(path))
                    return path;
            }
        return null;
    }

    // The name varies by build: shadPS4.exe (SDL), shadPS4QtLauncher.exe (Qt),
    // Shadps4-qt, an AppImage, or a shadPS4.app bundle. Try the known names, then
    // fall back to any shadPS4 program in the folder.
    private static string? FindProgramIn(string dir)
    {
        if (OperatingSystem.IsWindows())
        {
            foreach (string exact in new[] { "shadPS4.exe", "shadPS4QtLauncher.exe", "shadps4.exe" })
            {
                string path = Path.Combine(dir, exact);
                if (File.Exists(path))
                    return path;
            }
            return Directory.EnumerateFiles(dir, "shad*.exe").FirstOrDefault();
        }

        if (OperatingSystem.IsMacOS())
        {
            string bundle = Path.Combine(dir, "shadPS4.app");
            if (Directory.Exists(bundle))
                return bundle;
            return Directory.EnumerateDirectories(dir, "shad*.app").FirstOrDefault();
        }

        // Linux: a named binary, an AppImage, or any shad* program.
        foreach (string exact in new[] { "shadps4", "Shadps4-qt", "shadPS4" })
        {
            string path = Path.Combine(dir, exact);
            if (File.Exists(path))
                return path;
        }
        return Directory.EnumerateFiles(dir, "*.AppImage").FirstOrDefault()
            ?? Directory.EnumerateFiles(dir, "shad*").FirstOrDefault(f => Path.GetExtension(f).Length == 0);
    }

    // Starts the shadPS4 program. A Flatpak app is run via `flatpak run`, a macOS
    // .app bundle via `open`, and anything else directly.
    public void Launch(string program)
    {
        if (program.StartsWith(FlatpakPrefix, StringComparison.Ordinal))
        {
            Process.Start(new ProcessStartInfo("flatpak", "run " + program[FlatpakPrefix.Length..]) { UseShellExecute = false });
            return;
        }

        if (OperatingSystem.IsMacOS() && program.EndsWith(".app"))
        {
            Process.Start(new ProcessStartInfo("open", $"\"{program}\"") { UseShellExecute = false });
            return;
        }

        Process.Start(new ProcessStartInfo(program)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(program) ?? string.Empty,
        });
    }

    // Iterative search that skips folders we can't read, rather than aborting the
    // whole walk the way EnumerateDirectories(..., AllDirectories) does on the
    // first UnauthorizedAccessException.
    private static IEnumerable<string> FindFoldersNamed(string root, string name)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var dir = pending.Pop();
            string[] children;
            try
            {
                children = Directory.GetDirectories(dir);
            }
            catch (Exception e) when (e is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
            {
                continue;
            }

            foreach (var child in children)
            {
                if (string.Equals(Path.GetFileName(child), name, StringComparison.OrdinalIgnoreCase))
                    yield return child;
                else
                    pending.Push(child);
            }
        }
    }
}
