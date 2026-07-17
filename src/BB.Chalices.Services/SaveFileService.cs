using BB.Chalices.Core.Binary;
using BB.Chalices.Core.Saves;

namespace BB.Chalices.Services;

// Holds the currently-open save and brokers reads/writes against it.
public class SaveFileService
{
    private SaveFile? _save;
    private string? _path;

    public string? CurrentPath => _path;

    public SaveFile Load(string path)
    {
        _save = SaveFile.LoadFromFile(path);
        _path = path;
        return _save;
    }

    // Write the edited bytes over the file. By default a rolling <dir>/backup/<name>.bak
    // is written first as a safety net; the caller can skip it when a managed backup was
    // already made (auto-backup), so a save never leaves two copies behind.
    public void Save(bool createBackup = true)
    {
        if (_save is null || string.IsNullOrEmpty(_path))
            throw new InvalidOperationException("No save file loaded");

        if (createBackup)
            SaveFile.CreateBackup(_path);
        _save.SaveToFile(_path);
    }

    // Reads just the hunter's name from a save file without making it the current
    // save. Returns null if the file can't be read or isn't a valid save.
    public static string? PeekCharacterName(string path)
    {
        try
        {
            byte[] data = System.IO.File.ReadAllBytes(path);
            int inventory = SaveFileReader.FindInventoryMarker(data);
            if (inventory < 0)
                return null;
            string name = SaveFileReader.GetCharacterName(data, inventory);
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch
        {
            return null;
        }
    }

    // Renames the loaded hunter; the change is written on the next Save.
    public void SetCharacterName(string name)
    {
        if (_save is null) throw new InvalidOperationException("No save file loaded");
        _save.SetCharacterName(name);
    }

    public uint Echoes => _save?.Echoes ?? 0;
    public uint Insight => _save?.Insight ?? 0;
    public uint Level => _save?.Level ?? 0;

    public void SetEchoes(uint value)
    {
        if (_save is null) throw new InvalidOperationException("No save file loaded");
        _save.SetEchoes(value);
    }

    public void SetInsight(uint value)
    {
        if (_save is null) throw new InvalidOperationException("No save file loaded");
        _save.SetInsight(value);
    }

    public void SetLevel(uint value)
    {
        if (_save is null) throw new InvalidOperationException("No save file loaded");
        _save.SetLevel(value);
    }

    public void SetSlot(int slot, byte[] dungeonBytes)
    {
        if (_save is null) throw new InvalidOperationException("No save file loaded");
        _save.SetSlot(slot, dungeonBytes);
    }

    // --- Headstone field editing (rites / poison / 4th layer) ---

    private SaveFile Loaded => _save ?? throw new InvalidOperationException("No save file loaded");

    public byte[] GetSlotBytes(int slot) => Loaded.GetSlotBytes(slot);

    public int SlotOffset(int slot) => _save?.GetSlotOffset(slot) ?? 0;

    public string SlotHexDump(int slot) => Loaded.HexDumpSlot(slot);

    public void SetRite(int slot, int riteIndex, Headstone.Rite rite)
    {
        var record = Loaded.GetSlotBytes(slot);
        Headstone.ApplyRite(record, riteIndex, rite);
        Loaded.WriteSlotRaw(slot, record);
    }

    public void SetPoison(int slot, bool enabled)
    {
        var record = Loaded.GetSlotBytes(slot);
        Headstone.ApplyPoison(record, enabled);
        Loaded.WriteSlotRaw(slot, record);
    }

    public void SetFourthLayer(int slot, bool open)
    {
        var record = Loaded.GetSlotBytes(slot);
        Headstone.ApplyFourthLayer(record, open);
        Loaded.WriteSlotRaw(slot, record);
    }

    public void SetSpecialEnemy(int slot, Headstone.SpecialEnemy option)
    {
        var record = Loaded.GetSlotBytes(slot);
        Headstone.ApplySpecialEnemy(record, option);
        Loaded.WriteSlotRaw(slot, record);
    }

    public void SetDifficulty(int slot, bool difficultyUp)
    {
        var record = Loaded.GetSlotBytes(slot);
        Headstone.ApplyDifficulty(record, difficultyUp);
        Loaded.WriteSlotRaw(slot, record);
    }

    public void ClearSlot(int slot) =>
        Loaded.WriteSlotRaw(slot, DungeonStructure.Empty().Data);

    // Write a single headstone field from a hex string. Returns false if the hex
    // is invalid or the wrong length for that field.
    public bool TrySetField(int slot, Headstone.HeadstoneField field, string hex)
    {
        if (!Headstone.TryParseField(hex, field, out var bytes))
            return false;

        var record = Loaded.GetSlotBytes(slot);
        bytes.CopyTo(record, field.Offset);
        Loaded.WriteSlotRaw(slot, record);
        return true;
    }
}
