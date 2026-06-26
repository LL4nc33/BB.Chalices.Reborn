namespace BB.Chalices.Core.Binary;

// Low-level reader for Bloodborne save files. Every offset here is relative to
// the inventory marker, whose absolute position varies between saves. The magic
// numbers come from Noxde's reverse-engineering of the userdata format.
public static class SaveFileReader
{
    private const int HeadstoneBase = 88328;
    private const int FlagsBase = 102704;
    private const int Stride = 0x7D;      // 125 bytes per chalice headstone
    private const int NameOffset = -469;  // character name, relative to the marker
    private const int MaxSlots = 6;

    // The inventory block opens with the bytes 40 F0 FF FF. Returns the marker's
    // offset, or -1 if it isn't there (not a valid save).
    public static int FindInventoryMarker(ReadOnlySpan<byte> data)
    {
        for (int i = 0; i < data.Length - 3; i += 4)
        {
            if (data[i] == 0x40 && data[i + 1] == 0xF0 && data[i + 2] == 0xFF && data[i + 3] == 0xFF)
                return i;
        }
        return -1;
    }

    public static int GetHeadstoneOffset(int inventory, int slot)
    {
        if (slot < 1 || slot > MaxSlots)
            throw new ArgumentOutOfRangeException(nameof(slot), $"Slot must be 1-{MaxSlots}");

        return inventory + HeadstoneBase + (slot - 1) * Stride;
    }

    public static int GetFlagsOffset(int inventory, int slot)
    {
        if (slot < 1 || slot > MaxSlots)
            throw new ArgumentOutOfRangeException(nameof(slot), $"Slot must be 1-{MaxSlots}");

        return inventory + FlagsBase + (slot - 1) * Stride;
    }

    public static string GetCharacterName(ReadOnlySpan<byte> data, int inventory)
    {
        int offset = inventory + NameOffset;
        if (offset < 0 || offset + 32 > data.Length)
            return string.Empty;

        var name = data.Slice(offset, 32);
        int end = name.IndexOf((byte)0);
        if (end == -1) end = 32;

        return System.Text.Encoding.ASCII.GetString(name[..end]);
    }

    // We read up to the flags region, so reject anything that can't hold it.
    public static bool ValidateSaveFileSize(int fileSize, int inventory) =>
        fileSize >= inventory + 103000;
}
