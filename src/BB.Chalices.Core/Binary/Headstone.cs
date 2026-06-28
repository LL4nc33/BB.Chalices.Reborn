namespace BB.Chalices.Core.Binary;

// Rite / poison / 4th-layer editing for a single 125-byte headstone record.
//
// Every byte preset and the per-dungeon-type rules below are reverse-engineered
// values carried over verbatim from the OidaNice Custom Chalices editor. The game
// reads these exact bytes, so they must not be changed, only understood.
//
// All offsets are relative to the start of the 125-byte record.
public static class Headstone
{
    public const int JoinRequirementsOffset = 0x10;
    public const int FourthLayerOffset = 0x2C;
    public const int PoisonOffset = 0x34;
    public const int RiteSlot1Offset = 0x3C;
    public const int RiteSlot2Offset = 0x44;
    public const int RiteSlot3Offset = 0x4C;
    public const int RiteSlot4Offset = 0x54;
    public const int FieldLength = 8;

    public static readonly int[] RiteSlotOffsets =
        [RiteSlot1Offset, RiteSlot2Offset, RiteSlot3Offset, RiteSlot4Offset];

    // --- Rites (8-byte presets) ---------------------------------------------

    public enum Rite { None, Fetid, Rotted, Cursed, Sinister }

    public static byte[] RiteBytes(Rite rite) => rite switch
    {
        Rite.Fetid    => FromHex("00009CAE0000000B"),
        Rite.Rotted   => FromHex("00009CB80000001B"),
        Rite.Cursed   => FromHex("00009CEA00000049"),
        Rite.Sinister => FromHex("0000000000000028"),
        _             => FromHex("FFFFFFFFFFFFFFFF"), // empty slot
    };

    public static Rite ReadRite(ReadOnlySpan<byte> record, int riteSlotOffset)
    {
        if (record.Length < riteSlotOffset + FieldLength)
            return Rite.None;

        var slot = record.Slice(riteSlotOffset, FieldLength);
        foreach (var rite in (Rite[])[Rite.Fetid, Rite.Rotted, Rite.Cursed, Rite.Sinister])
        {
            if (slot.SequenceEqual(RiteBytes(rite)))
                return rite;
        }
        return Rite.None;
    }

    // --- Dungeon type (from the map byte at offset 1) ------------------------

    public static string DungeonType(ReadOnlySpan<byte> record)
    {
        if (record.Length < 2)
            return "Unknown";

        return record[1] switch
        {
            0x0A => "Pthumeru 1",
            0x14 => "Pthumeru 2",
            0x15 => "Hintertomb 2",
            0x1E => "Pthumeru 3",
            0x1F => "Hintertomb 3",
            0x28 => "Pthumeru 4",
            0x2A => "Loran 4",
            0x32 => "Pthumeru 5",
            0x34 => "Loran 5",
            0x35 => "Isz 5",
            _    => "Unknown",
        };
    }

    // --- 4th layer ----------------------------------------------------------
    // Only the last byte of the 8-byte field changes; the rest is a fixed prefix.

    private static readonly byte[] FourthLayerPrefix = [0x00, 0x97, 0xDF, 0x24, 0x00, 0x00, 0x00];

    public static byte[] FourthLayerBytes(byte controlByte)
    {
        var result = new byte[FieldLength];
        FourthLayerPrefix.CopyTo(result, 0);
        result[7] = controlByte;
        return result;
    }

    public static byte[] NoFourthLayer() => FromHex("FFFFFFFFFFFFFFFF");

    // A dungeon's 4th-layer ability follows its actual area (byte 0x01) and whether
    // it is Sinister (layout seed 0x02 in 14/15), never the editable join req, so
    // false-depth dungeons keep their real layout's behaviour. Sinister chalices and
    // Pthumeru 1 never have a 4th layer. (Tomb Prospectors random-effects report.)
    public static (bool possible, byte open, byte closed) FourthLayerControl(ReadOnlySpan<byte> record)
    {
        if (record.Length < 3 || record[2] is 0x14 or 0x15) // too short, or Sinister
            return (false, 0x00, 0x00);

        return record[1] switch
        {
            0x14 or 0x15 or 0x1F or 0x2A or 0x34 => (true, 0x3C, 0xFF), // Pthumeru 2, Hintertomb 2/3, Loran 4/5
            0x1E or 0x28 or 0x32                  => (true, 0x3D, 0x3E), // Pthumeru 3/4/5
            0x35                                   => (true, 0x43, 0x44), // Isz 5
            _                                      => (false, 0x00, 0x00), // Pthumeru 1 and unknown
        };
    }

    private static readonly byte[] FourthLayerOpenBytes = [0x3C, 0x3D, 0x3F, 0x41, 0x43];

    public static bool IsFourthLayerOpen(ReadOnlySpan<byte> record)
    {
        if (record.Length < FourthLayerOffset + FieldLength)
            return false;
        return Array.IndexOf(FourthLayerOpenBytes, record[FourthLayerOffset + 7]) >= 0;
    }

    public static bool FourthLayerPossible(ReadOnlySpan<byte> record) =>
        FourthLayerControl(record).possible;

    // --- Poison -------------------------------------------------------------
    // Same shape: a fixed template with the poison byte in the last position.

    public static byte[] PoisonBytes(byte controlByte)
    {
        var result = FromHex("FFFFFFFFFFFFFFFF");
        result[7] = controlByte;
        return result;
    }

    public static byte[] NoPoison() => FromHex("FFFFFFFFFFFFFFFF");

    // Which dungeon types can be poisoned, by area byte: Hintertomb 2/3 and Pthumeru
    // 4/5 can, Isz is always non-poison, Pthumeru 1-3 and Loran never are. Sinister
    // shares the same bytes. (Tomb Prospectors random-effects report.)
    public static string PoisonDungeonType(ReadOnlySpan<byte> record) => record.Length < 2 ? "Other" : record[1] switch
    {
        0x15 => "Hintertomb2",
        0x1F => "Hintertomb3",
        0x28 => "Pthumeru4",
        0x32 => "Pthumeru5",
        0x35 => "Isz5",
        _    => "Other",
    };

    public static byte ExpectedPoisonByte(string dungeonType, bool poisonEnabled) => dungeonType switch
    {
        "Hintertomb2" or "Hintertomb3" => poisonEnabled ? (byte)0x0A : (byte)0xFF, // 0A on, FF off
        "Pthumeru4" or "Pthumeru5"     => poisonEnabled ? (byte)0x0D : (byte)0x0E, // 0D on, 0E off
        "Isz5"                          => (byte)0x0F,                              // always off
        _                               => (byte)0xFF,                              // no poison
    };

    public static byte[] SmartPoison(ReadOnlySpan<byte> record, bool poisonEnabled)
    {
        var type = PoisonDungeonType(record);
        return PoisonBytes(ExpectedPoisonByte(type, poisonEnabled));
    }

    public static bool PoisonPossible(ReadOnlySpan<byte> record)
    {
        string type = PoisonDungeonType(record);
        // Isz dungeons are always non-poison (the byte stays 0x0F), so the toggle
        // would be a no-op; treat Isz5 like "Other" for the enable check.
        return type is not "Other" and not "Isz5";
    }

    public static bool IsPoisoned(ReadOnlySpan<byte> record)
    {
        if (record.Length < PoisonOffset + FieldLength)
            return false;
        return record[PoisonOffset + 7] is 0x0A or 0x0D; // the two "on" bytes
    }

    // The chalice an edited dungeon claims to require, decoded from its join hex
    // (from the Tomb Prospectors Hex Research Central join-requirements table).
    public static string JoinRequirementsLabel(string joinHex) => joinHex switch
    {
        "000017DD" => "Pthumeru 1",
        "000017D4" => "Pthumeru 1 root",
        "00001841" => "Pthumeru 2",
        "00001838" => "Pthumeru 2 root",
        "0000184B" => "Hintertomb 2",
        "00001842" => "Hintertomb 2 root",
        "000018A5" => "Pthumeru 3",
        "0000189C" => "Pthumeru 3 root",
        "0000189E" => "Pthumeru 3 sinister root",
        "000018AF" => "Hintertomb 3",
        "000018A6" => "Hintertomb 3 root",
        "000018A8" => "Hintertomb 3 sinister root",
        "00001909" => "Pthumeru 4",
        "00001901" => "Pthumeru 4 root",
        "0000191D" => "Loran 4",
        "00001914" => "Loran 4 root",
        "0000196D" => "Pthumeru 5",
        "00001964" => "Pthumeru 5 root",
        "00001966" => "Pthumeru 5 sinister root",
        "00001981" => "Loran 5",
        "00001978" => "Loran 5 root",
        "0000197A" => "Loran 5 sinister root",
        "0000198B" => "Isz 5",
        "00001982" => "Isz 5 root",
        "00001984" => "Isz 5 sinister root",
        _ => "Unknown",
    };

    // --- Helpers ------------------------------------------------------------

    public static string JoinRequirementsHex(ReadOnlySpan<byte> record)
    {
        if (record.Length < JoinRequirementsOffset + 4)
            return string.Empty;
        return Convert.ToHexString(record.Slice(JoinRequirementsOffset, 4));
    }

    public static byte[] FromHex(string hex) => Convert.FromHexString(hex);

    // --- Advanced: the individual headstone fields ---

    public sealed record HeadstoneField(string Name, int Offset, int Length);

    public static readonly IReadOnlyList<HeadstoneField> Fields =
    [
        new("Map hex", 0x00, 4),
        new("Dungeon id", 0x04, 8),
        new("Map hex (2)", 0x0C, 4),
        new("Join requirements", 0x10, 4),
        new("Special enemy / shop", 0x14, 8),
        new("Unique item", 0x1C, 8),
        new("Gem effect", 0x24, 8),
        new("4th layer", 0x2C, 8),
        new("Poison", 0x34, 8),
        new("Rite slot 1", 0x3C, 8),
        new("Rite slot 2", 0x44, 8),
        new("Rite slot 3", 0x4C, 8),
        new("Rite slot 4", 0x54, 8),
        new("Creator PSN", 0x5C, 16),
        new("Character name", 0x6C, 16),
    ];

    public static string ReadFieldHex(ReadOnlySpan<byte> record, HeadstoneField field) =>
        Convert.ToHexString(record.Slice(field.Offset, field.Length));

    // Parse a hex string for a field. Whitespace is ignored, but it must be exactly
    // the field's length. Returns false on bad hex or the wrong length.
    public static bool TryParseField(string? hex, HeadstoneField field, out byte[] bytes)
    {
        bytes = [];
        var clean = new string((hex ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (clean.Length != field.Length * 2)
            return false;

        try
        {
            bytes = Convert.FromHexString(clean);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // HxD-style dump of the record's bytes in their file context: bytes outside
    // the record show as "..", aligned to 16-byte rows. Used for the live view.
    public static string HexDump(ReadOnlySpan<byte> fileBytes, int recordStart, int recordLength)
    {
        const int perLine = 16;
        int dumpStart = recordStart & ~0xF;
        int dumpEnd = (recordStart + recordLength + 0xF) & ~0xF;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Offset(h)  00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
        sb.AppendLine("-----------------------------------------------------------");

        for (int line = dumpStart; line < dumpEnd; line += perLine)
        {
            sb.Append($"{line:X8}   ");
            for (int i = 0; i < perLine; i++)
            {
                int at = line + i;
                bool inRecord = at >= recordStart && at < recordStart + recordLength && at < fileBytes.Length;
                sb.Append(inRecord ? $"{fileBytes[at]:X2} " : ".. ");
            }
            sb.Append(' ');
            for (int i = 0; i < perLine; i++)
            {
                int at = line + i;
                bool inRecord = at >= recordStart && at < recordStart + recordLength && at < fileBytes.Length;
                if (!inRecord) { sb.Append('.'); continue; }
                byte b = fileBytes[at];
                sb.Append(b is >= 32 and <= 126 ? (char)b : '.');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
