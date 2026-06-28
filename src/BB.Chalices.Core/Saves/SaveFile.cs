using BB.Chalices.Core.Binary;

namespace BB.Chalices.Core.Saves;

// An in-memory Bloodborne save. The inventory marker is located once on
// construction; slots, the character name and the discovery flags are all
// addressed relative to it.
public class SaveFile
{
    private readonly byte[] _data;
    private readonly int _inventoryOffset;

    public SaveFile(byte[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));

        _inventoryOffset = SaveFileReader.FindInventoryMarker(_data);
        if (_inventoryOffset == -1)
            throw new InvalidOperationException("Save file does not contain inventory marker");

        if (!SaveFileReader.ValidateSaveFileSize(_data.Length, _inventoryOffset))
            throw new InvalidOperationException("Save file is too small or corrupted");
    }

    public string CharacterName => SaveFileReader.GetCharacterName(_data, _inventoryOffset);

    // Renames the hunter in place; the change is written on the next Save.
    public void SetCharacterName(string name) => SaveFileReader.SetCharacterName(_data, _inventoryOffset, name);

    public int InventoryOffset => _inventoryOffset;

    public DungeonStructure GetSlot(int slot)
    {
        int offset = SaveFileReader.GetHeadstoneOffset(_inventoryOffset, slot);
        var bytes = new byte[DungeonStructure.Size];
        Array.Copy(_data, offset, bytes, 0, DungeonStructure.Size);
        return new DungeonStructure(bytes);
    }

    public void SetSlot(int slot, DungeonStructure dungeon)
    {
        int offset = SaveFileReader.GetHeadstoneOffset(_inventoryOffset, slot);
        dungeon.Data.CopyTo(_data.AsSpan(offset, DungeonStructure.Size));
        SetDiscoveryFlag(slot);
    }

    public void SetSlot(int slot, byte[] dungeonBytes)
    {
        if (dungeonBytes.Length != DungeonStructure.Size)
            throw new ArgumentException($"Dungeon data must be {DungeonStructure.Size} bytes");

        int offset = SaveFileReader.GetHeadstoneOffset(_inventoryOffset, slot);
        Array.Copy(dungeonBytes, 0, _data, offset, DungeonStructure.Size);
        SetDiscoveryFlag(slot);
    }

    // Writing a dungeon also flips the matching "discovered" flag so the game
    // shows the altar as available. These leading bytes are the known-good pattern.
    private void SetDiscoveryFlag(int slot)
    {
        int flagOffset = SaveFileReader.GetFlagsOffset(_inventoryOffset, slot);

        var flag = new byte[DungeonStructure.Size];
        flag[0] = 0x30;
        flag[1] = 0x00;
        flag[2] = 0x03;
        flag[3] = 0xE8;
        flag[4] = 0x00;
        flag[5] = 0x04;
        flag[16] = 0x03; // the second part of the discovery pattern; the
        flag[17] = 0x02; // original index.js setFlag writes it and the game needs it

        Array.Copy(flag, 0, _data, flagOffset, DungeonStructure.Size);
    }

    public byte[] GetBytes() => _data;

    // --- Field-level access for the headstone editor ---

    public int GetSlotOffset(int slot) => SaveFileReader.GetHeadstoneOffset(_inventoryOffset, slot);

    public byte[] GetSlotBytes(int slot) =>
        _data.AsSpan(GetSlotOffset(slot), DungeonStructure.Size).ToArray();

    // Write a 125-byte record straight into a slot without touching the discovery
    // flags. Used when editing rites/poison/4th-layer on an already-placed dungeon.
    public void WriteSlotRaw(int slot, ReadOnlySpan<byte> record)
    {
        if (record.Length != DungeonStructure.Size)
            throw new ArgumentException($"Record must be {DungeonStructure.Size} bytes", nameof(record));

        record.CopyTo(_data.AsSpan(GetSlotOffset(slot), DungeonStructure.Size));
    }

    public string HexDumpSlot(int slot) =>
        Headstone.HexDump(_data, GetSlotOffset(slot), DungeonStructure.Size);

    public void SaveToFile(string path) => File.WriteAllBytes(path, _data);

    public static SaveFile LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Save file not found", path);

        return new SaveFile(File.ReadAllBytes(path));
    }

    // Copies the file to <dir>/backup/<name>.bak before it gets overwritten.
    public static void CreateBackup(string savePath)
    {
        if (!File.Exists(savePath))
            throw new FileNotFoundException("Save file not found", savePath);

        string dir = Path.GetDirectoryName(savePath) ?? "";
        string backupDir = Path.Combine(dir, "backup");
        Directory.CreateDirectory(backupDir);

        string backupPath = Path.Combine(backupDir, $"{Path.GetFileName(savePath)}.bak");
        File.Copy(savePath, backupPath, overwrite: true);
    }
}
