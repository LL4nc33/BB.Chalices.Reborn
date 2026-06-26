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

    public string DisplayName =>
        string.IsNullOrEmpty(Description) ? Glyph : $"{Glyph} - {Description}";
}
