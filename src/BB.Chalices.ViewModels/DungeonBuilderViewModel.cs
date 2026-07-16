using System.Collections.ObjectModel;
using BB.Chalices.Core.Binary;
using BB.Chalices.Services;
using ReactiveUI;

namespace BB.Chalices.ViewModels;

// Builds a fresh dungeon from friendly parameters. It draws a known-good base for
// the chosen area from the catalogue, so the result keeps a valid serial, and only
// the structural bytes are changed. Rites and effects are then set in the editor.
public class DungeonBuilderViewModel : ViewModelBase
{
    private readonly DungeonService _dungeons;
    private Dictionary<byte, byte[]> _basesByArea = new();

    public DungeonBuilderViewModel(DungeonService dungeons)
    {
        _dungeons = dungeons;
        Areas = new ObservableCollection<DungeonBuilder.Area>(DungeonBuilder.Areas);
        Variants = new ObservableCollection<DungeonBuilder.Variant>(Enum.GetValues<DungeonBuilder.Variant>());
        _selectedArea = Areas[^1]; // Isz 5, the usual gem-farming area
        _selectedVariant = DungeonBuilder.Variant.Normal;
    }

    public ObservableCollection<DungeonBuilder.Area> Areas { get; }
    public ObservableCollection<DungeonBuilder.Variant> Variants { get; }

    private DungeonBuilder.Area _selectedArea;
    public DungeonBuilder.Area SelectedArea
    {
        get => _selectedArea;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedArea, value);
            this.RaisePropertyChanged(nameof(SecondMapPossible));
            if (!value.HasTwoMaps)
                SecondMap = false;
            UpdatePreview();
        }
    }

    private DungeonBuilder.Variant _selectedVariant;
    public DungeonBuilder.Variant SelectedVariant
    {
        get => _selectedVariant;
        set { this.RaiseAndSetIfChanged(ref _selectedVariant, value); UpdatePreview(); }
    }

    private bool _secondMap;
    public bool SecondMap
    {
        get => _secondMap;
        set { this.RaiseAndSetIfChanged(ref _secondMap, value); UpdatePreview(); }
    }

    public bool SecondMapPossible => SelectedArea.HasTwoMaps;

    private int _dungeonNumber;
    public int DungeonNumber
    {
        get => _dungeonNumber;
        set { this.RaiseAndSetIfChanged(ref _dungeonNumber, Math.Clamp(value, 0, 99)); UpdatePreview(); }
    }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        private set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    // --- Readable preview: what this dungeon actually is, decoded from the built bytes ---

    private byte[]? _record;
    public byte[]? Record => _record;
    public bool CanPlace => _record is not null;

    private string? _previewCoffin;
    public string? PreviewCoffin { get => _previewCoffin; private set => this.RaiseAndSetIfChanged(ref _previewCoffin, value); }

    private string? _previewGems;
    public string? PreviewGems { get => _previewGems; private set => this.RaiseAndSetIfChanged(ref _previewGems, value); }

    private string _previewRequires = string.Empty;
    public string PreviewRequires { get => _previewRequires; private set => this.RaiseAndSetIfChanged(ref _previewRequires, value); }

    // Jump to a random layout number so farmers can roll through coffins/gems quickly.
    public void RandomLayout() => DungeonNumber = System.Random.Shared.Next(0, 100);

    // Pick one normal dungeon (layout map 00 or 01) per area as the base.
    public async Task InitAsync()
    {
        var all = await _dungeons.GetAllAsync();
        _basesByArea = all
            .Where(d => d.Bytes.Length == DungeonStructure.Size)
            .GroupBy(d => d.Bytes[1])
            .ToDictionary(
                g => g.Key,
                g => (g.FirstOrDefault(d => d.Bytes[2] <= 0x01) ?? g.First()).Bytes);
        UpdatePreview();
    }

    public byte[]? Build()
    {
        if (!_basesByArea.TryGetValue(SelectedArea.MapByte, out var baseRecord))
            return null;

        int mapIndex = SecondMap && SelectedArea.HasTwoMaps ? 1 : 0;
        return DungeonBuilder.Build(baseRecord, SelectedArea.MapByte, SelectedVariant, mapIndex, DungeonNumber);
    }

    private void UpdatePreview()
    {
        var record = Build();
        _record = record;
        this.RaisePropertyChanged(nameof(CanPlace));

        if (record is null)
        {
            PreviewRequires = string.Empty;
            PreviewCoffin = PreviewGems = null;
            Status = $"No base dungeon for {SelectedArea.Name} is in the catalogue yet.";
            return;
        }

        PreviewCoffin = record.Length > 3 ? DungeonGroups.UniqueItem(record[1], record[2], record[3]) : null;
        PreviewGems = GemPool.Favoured(record) is { Length: > 0 } gems ? gems : null;
        PreviewRequires = Headstone.JoinRequirementsLabel(Headstone.JoinRequirementsHex(record));

        string map = SelectedArea.HasTwoMaps ? (SecondMap ? ", map 2" : ", map 1") : string.Empty;
        Status = $"{SelectedArea.Name} ({SelectedVariant}){map}, layout {DungeonNumber}";
    }
}
