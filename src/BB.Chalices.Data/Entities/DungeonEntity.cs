namespace BB.Chalices.Data.Entities;

public class DungeonEntity
{
    public int Id { get; set; }

    public required string Glyph { get; set; }

    public required string Category { get; set; }

    public string? Description { get; set; }

    // The raw 125-byte chalice record written into a save slot.
    public required byte[] Bytes { get; set; }
}
