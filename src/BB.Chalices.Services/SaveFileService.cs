using BB.Chalices.Core.Binary;
using BB.Chalices.Core.SaveFile;

namespace BB.Chalices.Services;

// Holds the currently-open save and brokers reads/writes against it.
public class SaveFileService
{
    private SaveFile? _save;
    private string? _path;

    public SaveFile? CurrentSave => _save;
    public string? CurrentPath => _path;
    public bool HasLoadedSave => _save is not null;

    public SaveFile Load(string path)
    {
        _save = SaveFile.LoadFromFile(path);
        _path = path;
        return _save;
    }

    // Back up the file, then write the edited bytes over it.
    public void Save()
    {
        if (_save is null || string.IsNullOrEmpty(_path))
            throw new InvalidOperationException("No save file loaded");

        SaveFile.CreateBackup(_path);
        _save.SaveToFile(_path);
    }

    public void SaveAs(string path)
    {
        if (_save is null)
            throw new InvalidOperationException("No save file loaded");

        _save.SaveToFile(path);
        _path = path;
    }

    public DungeonStructure GetSlot(int slot)
    {
        if (_save is null) throw new InvalidOperationException("No save file loaded");
        return _save.GetSlot(slot);
    }

    public void SetSlot(int slot, DungeonStructure dungeon)
    {
        if (_save is null) throw new InvalidOperationException("No save file loaded");
        _save.SetSlot(slot, dungeon);
    }

    public void SetSlot(int slot, byte[] dungeonBytes)
    {
        if (_save is null) throw new InvalidOperationException("No save file loaded");
        _save.SetSlot(slot, dungeonBytes);
    }

    public void Close()
    {
        _save = null;
        _path = null;
    }
}
