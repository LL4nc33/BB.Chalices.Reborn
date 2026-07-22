namespace BB.Chalices.Services;

// Watches the open save file and reports when something outside the app rewrites
// it (shadPS4 saving its own state, a manual restore, a second editor). The game
// writes in bursts, so changes are collapsed by a trailing debounce; the app mutes
// the watcher around its own writes so a save does not trigger a reload of itself.
//
// Changed is raised on a background thread - the caller marshals to its UI thread.
public sealed class SaveFileWatcher : IDisposable
{
    private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(800);

    private readonly Action _changed;
    private FileSystemWatcher? _watcher;
    private Timer? _debounce;
    private DateTime _mutedUntilUtc;

    public SaveFileWatcher(Action changed) => _changed = changed;

    // Watch a single file, replacing whatever was watched before.
    public void Watch(string path)
    {
        Stop();

        string? dir = Path.GetDirectoryName(path);
        string file = Path.GetFileName(path);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(file) || !Directory.Exists(dir))
            return;

        var watcher = new FileSystemWatcher(dir, file)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                         | NotifyFilters.FileName | NotifyFilters.CreationTime,
        };
        watcher.Changed += OnTouched;
        watcher.Created += OnTouched;
        watcher.Renamed += OnTouched;
        watcher.EnableRaisingEvents = true;
        _watcher = watcher;
    }

    public void Stop()
    {
        if (_watcher is null)
            return;

        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnTouched;
        _watcher.Created -= OnTouched;
        _watcher.Renamed -= OnTouched;
        _watcher.Dispose();
        _watcher = null;
    }

    // Ignore events for a moment, so the app's own write doesn't look external.
    public void Mute(TimeSpan window) => _mutedUntilUtc = DateTime.UtcNow + window;

    private void OnTouched(object sender, FileSystemEventArgs e)
    {
        if (DateTime.UtcNow < _mutedUntilUtc)
            return;

        // Re-arm a one-shot timer, so a burst of writes fires Changed once.
        _debounce ??= new Timer(_ => _changed());
        _debounce.Change(SettleDelay, Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
    {
        Stop();
        _debounce?.Dispose();
        _debounce = null;
    }
}
