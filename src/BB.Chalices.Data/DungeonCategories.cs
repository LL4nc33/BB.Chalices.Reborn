namespace BB.Chalices.Data;

// The single source of truth for how a dungeon's category maps to a built-in list.
public static class DungeonCategories
{
    public const string CustomCategory = "Custom";

    // Nox's curated categories, kept separate from the full Tomb Prospectors set.
    public static readonly IReadOnlySet<string> NoxCategories =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "farming", "equipment", "bloodgems", "testing" };

    public static bool IsNox(string category) => NoxCategories.Contains(category);

    public static bool IsCommunity(string category) =>
        !IsNox(category) && !string.Equals(category, CustomCategory, StringComparison.OrdinalIgnoreCase);
}
