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

    // open/closed control byte per dungeon type, keyed by Join Requirements hex.
    public static (bool possible, byte open, byte closed) FourthLayerControl(string joinHex) => joinHex switch
    {
        "0000184B" or "00001842"               => (true, 0x3C, 0xFF), // Hintertomb 2
        "000018AF" or "000018A6" or "000018A8" => (true, 0x3C, 0xFF), // Hintertomb 3
        "00001787" or "0000177E"               => (true, 0x3C, 0xFF), // Pthumeru 2
        "00001909" or "00001901"               => (true, 0x3D, 0x3E), // Pthumeru 4
        "0000196D" or "00001964" or "00001966" => (true, 0x3D, 0x3E), // Pthumeru 5 (Sinister: 0x5C)
        "000018C3" or "000018BA" or "000018BC" => (true, 0x3C, 0xFF), // Loran 4
        "00001927" or "0000191E" or "00001920" => (true, 0x3C, 0xFF), // Loran 5
        "0000198B" or "00001982" or "00001984" => (true, 0x43, 0x44), // Isz 5
        _                                       => (false, 0x00, 0x00),
    };

    private static readonly byte[] FourthLayerOpenBytes = [0x3C, 0x3D, 0x3F, 0x41, 0x43];

    public static bool IsFourthLayerOpen(ReadOnlySpan<byte> record)
    {
        if (record.Length < FourthLayerOffset + FieldLength)
            return false;
        return Array.IndexOf(FourthLayerOpenBytes, record[FourthLayerOffset + 7]) >= 0;
    }

    public static bool FourthLayerPossible(ReadOnlySpan<byte> record) =>
        FourthLayerControl(JoinRequirementsHex(record)).possible;

    // --- Poison -------------------------------------------------------------
    // Same shape: a fixed template with the poison byte in the last position.

    public static byte[] PoisonBytes(byte controlByte)
    {
        var result = FromHex("FFFFFFFFFFFFFFFF");
        result[7] = controlByte;
        return result;
    }

    public static byte[] NoPoison() => FromHex("FFFFFFFFFFFFFFFF");

    // Which dungeon types can be poisoned, and the on/off byte per type.
    public static string PoisonDungeonType(string joinHex) => joinHex switch
    {
        "0000184B" or "00001842"               => "Hintertomb2",
        "000018AF" or "000018A6" or "000018A8" => "Hintertomb3",
        "00001909" or "00001901"               => "Pthumeru4",
        "0000196D" or "00001964" or "00001966" => "Pthumeru5",
        "0000198B" or "00001982" or "00001984" => "Isz5",
        _                                       => "Other",
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
        var type = PoisonDungeonType(JoinRequirementsHex(record));
        return PoisonBytes(ExpectedPoisonByte(type, poisonEnabled));
    }

    public static bool PoisonPossible(ReadOnlySpan<byte> record) =>
        PoisonDungeonType(JoinRequirementsHex(record)) != "Other";

    public static bool IsPoisoned(ReadOnlySpan<byte> record)
    {
        if (record.Length < PoisonOffset + FieldLength)
            return false;
        return record[PoisonOffset + 7] is 0x0A or 0x0D; // the two "on" bytes
    }

    // --- Helpers ------------------------------------------------------------

    public static string JoinRequirementsHex(ReadOnlySpan<byte> record)
    {
        if (record.Length < JoinRequirementsOffset + 4)
            return string.Empty;
        return Convert.ToHexString(record.Slice(JoinRequirementsOffset, 4));
    }

    public static byte[] FromHex(string hex) => Convert.FromHexString(hex);

    // Clean hex view of the record: a relative offset and 16 bytes per row,
    // narrow enough to read the whole 125-byte record without scrolling sideways.
    public static string CompactDump(ReadOnlySpan<byte> record, int perLine = 16)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < record.Length; i += perLine)
        {
            sb.Append($"{i:X2}:  ");

            int count = Math.Min(perLine, record.Length - i);
            for (int j = 0; j < count; j++)
                sb.Append($"{record[i + j]:X2} ");

            sb.AppendLine();
        }
        return sb.ToString();
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
