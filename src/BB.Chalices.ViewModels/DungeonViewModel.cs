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

    public string DisplayName =>
        string.IsNullOrEmpty(Description) ? Glyph : $"{Glyph} - {Description}";
}
