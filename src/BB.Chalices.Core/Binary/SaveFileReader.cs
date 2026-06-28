namespace BB.Chalices.Core.Binary;

// Low-level reader for Bloodborne save files. Every offset here is relative to
// the inventory marker, whose absolute position varies between saves. The magic
// numbers come from Noxde's reverse-engineering of the userdata format.
public static class SaveFileReader
{
    private const int HeadstoneBase = 88328;
    private const int FlagsBase = 102704;
    private const int Stride = 0x7D;      // 125 bytes per chalice headstone
    private const int NameOffset = -469;  // username field, relative to the marker
    private const int NameStringOffset = NameOffset + 1; // the UTF-16 string begins one byte in
    private const int NameMaxChars = 16;  // up to 16 characters, stored as UTF-16LE
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

    // The hunter's name is stored as UTF-16LE (two bytes per character), so the
    // old ASCII read stopped at the first character's null high byte. Matches
    // Noxde's Bloodborne-save-editor (username field at marker - 468, 16 chars).
    public static string GetCharacterName(ReadOnlySpan<byte> data, int inventory)
    {
        int offset = inventory + NameStringOffset;
        if (offset < 0 || offset + NameMaxChars * 2 > data.Length)
            return string.Empty;

        string raw = System.Text.Encoding.Unicode.GetString(data.Slice(offset, NameMaxChars * 2));
        int end = raw.IndexOf('\0');
        return (end >= 0 ? raw[..end] : raw).Trim();
    }

    // Overwrites the hunter's name in place (ASCII characters, padded with zeros).
    public static void SetCharacterName(Span<byte> data, int inventory, string name)
    {
        int offset = inventory + NameStringOffset;
        if (offset < 0 || offset + NameMaxChars * 2 > data.Length)
            return;

        byte[] ascii = System.Text.Encoding.ASCII.GetBytes(name);
        for (int i = 0; i < NameMaxChars; i++)
        {
            data[offset + i * 2] = i < ascii.Length ? ascii[i] : (byte)0;
            data[offset + i * 2 + 1] = 0; // high byte of the UTF-16 character
        }
    }

    // Writing a slot stamps a 125-byte discovery flag at GetFlagsOffset(inventory, slot);
    // reject anything that cannot hold every byte SetSlot would write for slot 6.
    public static bool ValidateSaveFileSize(int fileSize, int inventory) =>
        fileSize >= GetFlagsOffset(inventory, MaxSlots) + Stride;
}
