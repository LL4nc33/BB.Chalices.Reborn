namespace BB.Chalices.Core.Binary;

// A single chalice dungeon as it sits in a save slot: a fixed 125-byte record.
// The field map is from Noxde's notes on the userdata format.
public readonly struct DungeonStructure
{
    public const int Size = 125; // 0x7D

    private readonly byte[] _data;

    public DungeonStructure(byte[] data)
    {
        if (data.Length != Size)
            throw new ArgumentException($"Dungeon data must be exactly {Size} bytes", nameof(data));

        _data = data;
    }

    public ReadOnlySpan<byte> Data => _data;

    public ReadOnlySpan<byte> MapHex => Data[0..4];
    public ReadOnlySpan<byte> DungeonId => Data[4..12];
    public ReadOnlySpan<byte> JoinRequirements => Data[0x10..0x14];
    public ReadOnlySpan<byte> SpecialEnemy => Data[0x14..0x1C];
    public ReadOnlySpan<byte> UniqueItem => Data[0x1C..0x24];
    public ReadOnlySpan<byte> GemEffect => Data[0x24..0x2C];
    public ReadOnlySpan<byte> FourthLayer => Data[0x2C..0x34];
    public ReadOnlySpan<byte> Poison => Data[0x34..0x3C];
    public ReadOnlySpan<byte> RiteSlot1 => Data[0x3C..0x44];
    public ReadOnlySpan<byte> RiteSlot2 => Data[0x44..0x4C];
    public ReadOnlySpan<byte> RiteSlot3 => Data[0x4C..0x54];
    public ReadOnlySpan<byte> RiteSlot4 => Data[0x54..0x5C];
    public ReadOnlySpan<byte> CreatorPSN => Data[0x5C..0x6C];
    public ReadOnlySpan<byte> CharacterName => Data[0x6C..0x7C];

    // An empty altar slot, exactly as the game writes one: 0xFFFFFFFF map, a 0x80
    // marker at offset 8, every effect/rite field defaulted to 0xFF (0x0C-0x5B), a
    // zeroed creator/name block (0x5C-0x7B) and a 0x02 terminator at offset 0x7C.
    public static DungeonStructure Empty()
    {
        var data = new byte[Size];
        data[0] = data[1] = data[2] = data[3] = 0xFF;
        data[8] = 0x80;
        for (int i = 0x0C; i <= 0x5B; i++)
            data[i] = 0xFF;
        data[0x7C] = 0x02;
        return new DungeonStructure(data);
    }

    public bool IsEmpty() =>
        Data[0] == 0xFF && Data[1] == 0xFF && Data[2] == 0xFF && Data[3] == 0xFF;
}
