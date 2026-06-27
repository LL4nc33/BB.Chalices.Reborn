namespace BB.Chalices.Core.Binary;

// Builds a dungeon record from friendly parameters. To stay loadable it starts
// from a known-good base dungeon (which carries a valid server serial and the
// effect strings) and overwrites only the well-understood structural bytes.
// Offsets and bytes are verified against the Tomb Prospectors "Hex Research
// Central" reference by DrAnger.
public static class DungeonBuilder
{
    public sealed record Area(string Name, byte MapByte, bool HasTwoMaps);

    // Area and depth pick the map byte at 0x01. Depth 4-5 chalices draw from two
    // maps (200 layouts), depth 1-3 from one (100 layouts).
    public static readonly IReadOnlyList<Area> Areas =
    [
        new("Pthumeru 1", 0x0A, false),
        new("Pthumeru 2", 0x14, false),
        new("Hintertomb 2", 0x15, false),
        new("Pthumeru 3", 0x1E, false),
        new("Hintertomb 3", 0x1F, false),
        new("Pthumeru 4", 0x28, true),
        new("Loran 4", 0x2A, true),
        new("Pthumeru 5", 0x32, true),
        new("Loran 5", 0x34, true),
        new("Isz 5", 0x35, true),
    ];

    public enum Variant { Normal, Defiled, Sinister }

    // The layout-map byte (string 2, byte 0x02 mirrored at 0x0E): which dungeon map
    // the layout is drawn from. mapIndex is 0 for the first map, 1 for the second
    // (depth 4-5 only). Defiled and Sinister are offset variants of those maps.
    public static byte LayoutMapByte(Variant variant, int mapIndex) => variant switch
    {
        Variant.Defiled => (byte)(0x0A + mapIndex),
        Variant.Sinister => (byte)(0x14 + mapIndex),
        _ => (byte)mapIndex,
    };

    // Overwrite the structural bytes (area at 0x01, layout map at 0x02, dungeon
    // number at 0x03, both mirrored at 0x0D-0x0F) on a copy of a valid base record.
    // Everything else (serial, join requirements, effect strings) comes from the
    // base so the dungeon stays loadable.
    public static byte[] Build(ReadOnlySpan<byte> baseRecord, byte mapByte, Variant variant, int mapIndex, int dungeonNumber)
    {
        if (baseRecord.Length != DungeonStructure.Size)
            throw new ArgumentException($"Base record must be {DungeonStructure.Size} bytes.", nameof(baseRecord));

        byte layoutMap = LayoutMapByte(variant, mapIndex);
        var r = baseRecord.ToArray();
        r[0x01] = r[0x0D] = mapByte;
        r[0x02] = r[0x0E] = layoutMap;
        r[0x03] = r[0x0F] = (byte)dungeonNumber;
        return r;
    }
}
