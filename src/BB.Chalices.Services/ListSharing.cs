using BB.Chalices.Core.Sharing;
using BB.Chalices.Data.Entities;

namespace BB.Chalices.Services;

// Bridges a stored list to the portable share codec: a list becomes a set of named
// dungeons and back.
public static class ListSharing
{
    public static string Export(DungeonList list)
    {
        var items = new List<ShareItem>(list.Items.Count);
        foreach (var item in list.Items)
        {
            var dungeon = item.Dungeon;
            string name = string.IsNullOrWhiteSpace(dungeon.Description) ? dungeon.Glyph : dungeon.Description!;
            items.Add(new ShareItem(name, dungeon.Category, dungeon.Bytes));
        }
        return DungeonShare.Encode(new ShareSet(DungeonShare.CurrentVersion, items));
    }

    public static ShareSet Import(string? code)
    {
        if (!DungeonShare.TryDecode(code, out var set) || set.Items.Count == 0)
            throw new FormatException("That is not a dungeon share code.");
        return set;
    }
}
