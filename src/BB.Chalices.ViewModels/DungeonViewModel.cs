using BB.Chalices.Core.Binary;
using BB.Chalices.Data.Entities;

namespace BB.Chalices.ViewModels;

public class DungeonViewModel : ViewModelBase
{
    private readonly DungeonEntity _entity;

    public DungeonViewModel(DungeonEntity entity)
    {
        _entity = entity;
    }

    public int Id => _entity.Id;
    public string Glyph => _entity.Glyph;
    public string Category => _entity.Category;
    public string? Description => _entity.Description;
    public byte[] Bytes => _entity.Bytes;

    // Area and depth read from the map byte, for the catalogue.
    public string Type => Bytes.Length > 1 ? Headstone.DungeonType(Bytes) : "Unknown";

    // What the dungeon already carries, decoded from its bytes for the detail view.
    public string Rites
    {
        get
        {
            string joined = string.Join(", ", Headstone.RiteSlotOffsets
                .Select(offset => Headstone.ReadRite(Bytes, offset))
                .Where(rite => rite != Headstone.Rite.None));
            return string.IsNullOrEmpty(joined) ? "None" : joined;
        }
    }

    public bool IsPoisoned => Headstone.IsPoisoned(Bytes);
    public bool HasFourthLayer => Headstone.IsFourthLayerOpen(Bytes);

    // The chalice required to enter, decoded from the join requirements.
    public string JoinRequirement => Headstone.JoinRequirementsLabel(Headstone.JoinRequirementsHex(Bytes));

    // The unique coffin item this dungeon holds, from the dungeon-groups table.
    public string? UniqueItem => Bytes.Length > 3
        ? DungeonGroups.UniqueItem(Bytes[1], Bytes[2], Bytes[3])
        : null;

    // The gem effects this dungeon's gem pool favours (for farmers).
    public string? FavouredGems =>
        GemPool.Favoured(Bytes) is { Length: > 0 } gems ? gems : null;

    public string DisplayName =>
        string.IsNullOrEmpty(Description) ? Glyph : $"{Glyph} - {Description}";
}
