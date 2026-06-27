using BB.Chalices.Core.Binary;
using BB.Chalices.Core.Saves;

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

    // --- Headstone field editing (rites / poison / 4th layer) ---

    private SaveFile Loaded => _save ?? throw new InvalidOperationException("No save file loaded");

    public byte[] GetSlotBytes(int slot) => Loaded.GetSlotBytes(slot);

    public string SlotHexDump(int slot) => Loaded.HexDumpSlot(slot);

    public void SetRite(int slot, int riteIndex, Headstone.Rite rite)
    {
        var record = Loaded.GetSlotBytes(slot);
        Headstone.RiteBytes(rite).CopyTo(record, Headstone.RiteSlotOffsets[riteIndex]);
        Loaded.WriteSlotRaw(slot, record);
    }

    public void SetPoison(int slot, bool enabled)
    {
        var record = Loaded.GetSlotBytes(slot);
        Headstone.SmartPoison(record, enabled).CopyTo(record, Headstone.PoisonOffset);
        Loaded.WriteSlotRaw(slot, record);
    }

    public void SetFourthLayer(int slot, bool open)
    {
        var record = Loaded.GetSlotBytes(slot);
        var (possible, openByte, closedByte) = Headstone.FourthLayerControl(Headstone.JoinRequirementsHex(record));
        if (!possible)
            return;

        Headstone.FourthLayerBytes(open ? openByte : closedByte).CopyTo(record, Headstone.FourthLayerOffset);
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

    public void Close()
    {
        _save = null;
        _path = null;
    }
}
