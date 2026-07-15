namespace BB.Chalices.Data.Entities;

public class DungeonListItem
{
    public int Id { get; set; }

    public int ListId { get; set; }
    public DungeonList List { get; set; } = null!;

    public int DungeonId { get; set; }
    public DungeonEntity Dungeon { get; set; } = null!;

    public int Position { get; set; }
}
