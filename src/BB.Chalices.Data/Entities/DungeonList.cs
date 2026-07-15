namespace BB.Chalices.Data.Entities;

public enum ListSource { Bundled, Nox, User }

public class DungeonList
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ListSource Source { get; set; }

    public List<DungeonListItem> Items { get; set; } = new();

    // Only the user's own lists can be edited; built-in lists are rebuilt from source.
    public bool ReadOnly => Source != ListSource.User;
}
