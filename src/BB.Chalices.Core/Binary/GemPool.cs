namespace BB.Chalices.Core.Binary;

// Which gem effects a dungeon's gem pool favours, from the Tomb Prospectors
// "Gem Pools" research. Each effect string's functional byte can switch on a
// GemCategory that boosts a group of gem effects; a dungeon's pool is the union
// of the categories its bytes activate. We surface the headline of each category
// rather than the full statistical pool.
public static class GemPool
{
    // GemCategory id -> the gem effects it favours, in plain language.
    private static readonly Dictionary<int, string> CategoryHeadline = new()
    {
        [11] = "Physical ATK %",
        [12] = "Durability / stamina",
        [13] = "Charge ATK %",
        [14] = "STR / SKL scaling",
        [22] = "Physical ATK % / poison",
        [23] = "Physical UP near death",
        [34] = "Fire / Bolt ATK %",
        [35] = "HP recovery",
        [45] = "Arcane / Physical ATK %",
    };

    // The functional byte of each effect string maps to a GemCategory. The same
    // byte value means different things in different strings, so this is keyed per
    // field offset. (DungeonFeatureParam / GemCategoryParam.)
    private static int Category(int fieldOffset, byte functional) => (fieldOffset, functional) switch
    {
        // Gem effect string (0x24)
        (0x24, 0x32) or (0x24, 0x35) or (0x24, 0x36) => 11,
        (0x24, 0x33) => 22,
        (0x24, 0x34) => 34,
        // Unique item string (0x1C)
        (0x1C, 0x24) => 12,
        (0x1C, 0x25) => 23,
        (0x1C, 0x26) => 35,
        (0x1C, 0x27) => 45,
        // 4th layer string (0x2C)
        (0x2C, >= 0x3D and <= 0x42) or (0x2C, 0x5C) => 13,
        (0x2C, 0x43) or (0x2C, 0x44) => 12,
        // Poison string (0x34)
        (0x34, 0x0D) or (0x34, 0x0E) => 14,
        (0x34, 0x0F) => 22,
        // Special enemy / shop string (0x14) - Isz gem-category bytes
        (0x14, >= 0x4F and <= 0x54) => 34,
        _ => 0, // no gem category
    };

    private static readonly int[] EffectOffsets =
    [
        Headstone.SpecialEnemyOffset, Headstone.UniqueItemOffset, Headstone.GemEffectOffset,
        Headstone.FourthLayerOffset, Headstone.PoisonOffset,
    ];

    // The distinct GemCategory ids a dungeon's effect strings switch on.
    public static IReadOnlyList<int> ActiveCategories(ReadOnlySpan<byte> record)
    {
        var seen = new SortedSet<int>();
        foreach (int offset in EffectOffsets)
        {
            if (record.Length < offset + Headstone.FieldLength)
                continue;
            int category = Category(offset, record[offset + 7]);
            if (category != 0)
                seen.Add(category);
        }
        return seen.ToList();
    }

    // A short, de-duplicated summary of the gem effects this dungeon favours.
    public static string Favoured(ReadOnlySpan<byte> record)
    {
        var headlines = new List<string>();
        foreach (int category in ActiveCategories(record))
            if (CategoryHeadline.TryGetValue(category, out var headline) && !headlines.Contains(headline))
                headlines.Add(headline);

        return headlines.Count == 0 ? "" : string.Join(", ", headlines);
    }
}
