using System.Collections.ObjectModel;
using BB.Chalices.Core.Binary;
using BB.Chalices.Services;
using ReactiveUI;

namespace BB.Chalices.ViewModels;

// Builds a fresh dungeon from friendly parameters. It draws a known-good base for
// the chosen area from the catalogue, so the result keeps a valid serial, then sets
// the structural bytes plus any rites and effects you pick, all before it is placed.
public class DungeonBuilderViewModel : ViewModelBase
{
    private readonly DungeonService _dungeons;
    private Dictionary<byte, byte[]> _basesByArea = new();
    // Guards UpdatePreview from re-entering itself while it refreshes effect state.
    private bool _applying;

    public DungeonBuilderViewModel(DungeonService dungeons)
    {
        _dungeons = dungeons;
        Areas = new ObservableCollection<DungeonBuilder.Area>(DungeonBuilder.Areas);
        Variants = new ObservableCollection<DungeonBuilder.Variant>(Enum.GetValues<DungeonBuilder.Variant>());
        RiteSlots = new ObservableCollection<RiteSlotViewModel>(
            Enumerable.Range(0, 4).Select(i => new RiteSlotViewModel(i, (_, __) => UpdatePreview())));
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

    // --- Rites and effects: stamped onto the built record before it is placed ---

    public ObservableCollection<RiteSlotViewModel> RiteSlots { get; }

    // Warns about rite combinations that misbehave (shared with the slot editor).
    public string? RiteWarning => RiteWarnings.For(RiteSlots.Select(r => r.Rite));

    private bool _poisonPossible;
    public bool PoisonPossible { get => _poisonPossible; private set => this.RaiseAndSetIfChanged(ref _poisonPossible, value); }
    private bool _poisonEnabled;
    public bool PoisonEnabled { get => _poisonEnabled; set { this.RaiseAndSetIfChanged(ref _poisonEnabled, value); if (!_applying) UpdatePreview(); } }

    private bool _fourthLayerPossible;
    public bool FourthLayerPossible { get => _fourthLayerPossible; private set => this.RaiseAndSetIfChanged(ref _fourthLayerPossible, value); }
    private bool _fourthLayerOpen;
    public bool FourthLayerOpen { get => _fourthLayerOpen; set { this.RaiseAndSetIfChanged(ref _fourthLayerOpen, value); if (!_applying) UpdatePreview(); } }

    private bool _difficultyPossible;
    public bool DifficultyPossible { get => _difficultyPossible; private set => this.RaiseAndSetIfChanged(ref _difficultyPossible, value); }
    private bool _difficultyUp;
    public bool DifficultyUp { get => _difficultyUp; set { this.RaiseAndSetIfChanged(ref _difficultyUp, value); if (!_applying) UpdatePreview(); } }

    public ObservableCollection<Headstone.SpecialEnemy> SpecialEnemyOptions { get; } = new();
    private Headstone.SpecialEnemy? _selectedSpecialEnemy;
    public Headstone.SpecialEnemy? SelectedSpecialEnemy { get => _selectedSpecialEnemy; set { this.RaiseAndSetIfChanged(ref _selectedSpecialEnemy, value); if (!_applying) UpdatePreview(); } }

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

        if (record is null)
        {
            _record = null;
            this.RaisePropertyChanged(nameof(CanPlace));
            PreviewRequires = string.Empty;
            PreviewCoffin = PreviewGems = null;
            PoisonPossible = FourthLayerPossible = DifficultyPossible = false;
            SpecialEnemyOptions.Clear();
            Status = $"No base dungeon for {SelectedArea.Name} is in the catalogue yet.";
            return;
        }

        // Stamp the chosen rites first, so the effect options match the rited dungeon.
        foreach (var rite in RiteSlots)
            Headstone.ApplyRite(record, rite.Index, rite.Rite);
        this.RaisePropertyChanged(nameof(RiteWarning));

        // Refresh which effects this dungeon supports, and its special-enemy choices.
        _applying = true;
        PoisonPossible = Headstone.PoisonPossible(record);
        FourthLayerPossible = Headstone.FourthLayerPossible(record);
        DifficultyPossible = Headstone.DifficultyPossible(record);

        var natural = Headstone.ReadSpecialEnemy(record);
        SpecialEnemyOptions.Clear();
        foreach (var option in Headstone.SpecialEnemyOptions(record))
            SpecialEnemyOptions.Add(option);
        if (SelectedSpecialEnemy is null || !SpecialEnemyOptions.Contains(SelectedSpecialEnemy.Value))
            SelectedSpecialEnemy = SpecialEnemyOptions.Contains(natural) ? natural
                : SpecialEnemyOptions.Count > 0 ? SpecialEnemyOptions[0] : null;
        _applying = false;

        // Stamp the effect choices (each a no-op when not valid for this dungeon).
        if (PoisonPossible) Headstone.ApplyPoison(record, PoisonEnabled);
        if (FourthLayerPossible) Headstone.ApplyFourthLayer(record, FourthLayerOpen);
        if (DifficultyPossible) Headstone.ApplyDifficulty(record, DifficultyUp);
        if (SelectedSpecialEnemy is not null) Headstone.ApplySpecialEnemy(record, SelectedSpecialEnemy.Value);

        _record = record;
        this.RaisePropertyChanged(nameof(CanPlace));

        PreviewCoffin = record.Length > 3 ? DungeonGroups.UniqueItem(record[1], record[2], record[3]) : null;
        PreviewGems = GemPool.Favoured(record) is { Length: > 0 } gems ? gems : null;
        PreviewRequires = Headstone.JoinRequirementsLabel(Headstone.JoinRequirementsHex(record));

        string map = SelectedArea.HasTwoMaps ? (SecondMap ? ", map 2" : ", map 1") : string.Empty;
        Status = $"{SelectedArea.Name} ({SelectedVariant}){map}, layout {DungeonNumber}";
    }
}
