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

    // The usual places people unpack shadPS4 to. Returns null if none look right.
    public string? GuessShadPs4Root()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        string[] candidates =
        [
            Path.Combine(appData, "shadPS4"),
            Path.Combine(home, "shadPS4"),
            Path.Combine(home, "Downloads", "shadPS4"),
            Path.Combine(home, "Desktop", "shadPS4"),
            Path.Combine(home, "Documents", "shadPS4"),
        ];

        return Array.Find(candidates, c => Directory.Exists(Path.Combine(c, "user")));
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
