using System.Collections.ObjectModel;
using System.Reactive;
using BB.Chalices.Services;
using ReactiveUI;

namespace BB.Chalices.ViewModels;

public class MainViewModel : ViewModelBase
{
    private const string AllCategories = "All";

    private readonly SaveFileService _saves;
    private readonly DungeonService _dungeons;
    private readonly SaveLocatorService _locator;

    private List<DungeonViewModel> _all = new();

    private string? _characterName;
    private bool _hasLoadedSave;
    private string _statusMessage = "Open a save to begin.";
    private string _searchText = string.Empty;
    private string _selectedCategory = AllCategories;
    private DungeonViewModel? _selectedDungeon;
    private SlotViewModel? _selectedSlot;
    private DetectedSaveViewModel? _selectedDetectedSave;

    public MainViewModel(SaveFileService saves, DungeonService dungeons, SaveLocatorService locator)
    {
        _saves = saves;
        _dungeons = dungeons;
        _locator = locator;

        Dungeons = new ObservableCollection<DungeonViewModel>();
        Categories = new ObservableCollection<string> { AllCategories };
        Slots = new ObservableCollection<SlotViewModel>(Enumerable.Range(1, 6).Select(n => new SlotViewModel(n)));
        DetectedSaves = new ObservableCollection<DetectedSaveViewModel>();
        _selectedSlot = Slots[0];

        LoadDungeonsCommand = ReactiveCommand.CreateFromTask(LoadDungeonsAsync);
        LoadSaveCommand = ReactiveCommand.Create<string>(LoadSave);
        SaveCommand = ReactiveCommand.Create(Save);
        ApplyDungeonCommand = ReactiveCommand.Create(ApplyDungeon);
        DetectSavesCommand = ReactiveCommand.Create(DetectSaves);
    }

    public ObservableCollection<DungeonViewModel> Dungeons { get; }
    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<SlotViewModel> Slots { get; }
    public ObservableCollection<DetectedSaveViewModel> DetectedSaves { get; }

    public string? CharacterName
    {
        get => _characterName;
        private set => this.RaiseAndSetIfChanged(ref _characterName, value);
    }

    public bool HasLoadedSave
    {
        get => _hasLoadedSave;
        private set => this.RaiseAndSetIfChanged(ref _hasLoadedSave, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            ApplyFilter();
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            ApplyFilter();
        }
    }

    public DungeonViewModel? SelectedDungeon
    {
        get => _selectedDungeon;
        set => this.RaiseAndSetIfChanged(ref _selectedDungeon, value);
    }

    public SlotViewModel? SelectedSlot
    {
        get => _selectedSlot;
        set => this.RaiseAndSetIfChanged(ref _selectedSlot, value);
    }

    public DetectedSaveViewModel? SelectedDetectedSave
    {
        get => _selectedDetectedSave;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedDetectedSave, value);
            if (value is not null)
                LoadSave(value.Path);
        }
    }

    public ReactiveCommand<Unit, Unit> LoadDungeonsCommand { get; }
    public ReactiveCommand<string, Unit> LoadSaveCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> ApplyDungeonCommand { get; }
    public ReactiveCommand<Unit, Unit> DetectSavesCommand { get; }

    private async Task LoadDungeonsAsync()
    {
        var all = await _dungeons.GetAllAsync();
        _all = all.Select(d => new DungeonViewModel(d)).ToList();

        Categories.Clear();
        Categories.Add(AllCategories);
        foreach (var category in all.Select(d => d.Category).Distinct().OrderBy(c => c))
            Categories.Add(category);

        SelectedCategory = AllCategories; // also runs ApplyFilter
        StatusMessage = $"{_all.Count} dungeons ready.";
    }

    private void ApplyFilter()
    {
        IEnumerable<DungeonViewModel> query = _all;

        if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != AllCategories)
            query = query.Where(d => string.Equals(d.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));

        var search = SearchText?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(d =>
                d.Glyph.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (d.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

        Dungeons.Clear();
        foreach (var dungeon in query)
            Dungeons.Add(dungeon);
    }

    private void LoadSave(string path)
    {
        try
        {
            var save = _saves.Load(path);
            CharacterName = save.CharacterName;
            HasLoadedSave = true;
            RefreshSlots();
            StatusMessage = $"Loaded {System.IO.Path.GetFileName(path)} — Hunter “{save.CharacterName}”.";
        }
        catch (Exception ex)
        {
            HasLoadedSave = false;
            StatusMessage = $"Could not load save: {ex.Message}";
        }
    }

    private void Save()
    {
        try
        {
            _saves.Save();
            StatusMessage = "Saved. A backup was written next to the save (/backup).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    private void ApplyDungeon()
    {
        if (!HasLoadedSave)
        {
            StatusMessage = "Open a save first.";
            return;
        }
        if (SelectedDungeon is null)
        {
            StatusMessage = "Pick a dungeon from the list first.";
            return;
        }
        if (SelectedSlot is null)
        {
            StatusMessage = "Pick a slot first.";
            return;
        }

        _saves.SetSlot(SelectedSlot.Number, SelectedDungeon.Bytes);
        RefreshSlots();
        StatusMessage = $"Placed {SelectedDungeon.Glyph} in slot {SelectedSlot.Number}. Save to write it to disk.";
    }

    private void DetectSaves()
    {
        DetectedSaves.Clear();

        var root = _locator.GuessShadPs4Root();
        if (root is null)
        {
            StatusMessage = "Couldn't find a shadPS4 folder — use Open Save to browse manually.";
            return;
        }

        foreach (var folder in _locator.FindSaveFolders(root))
            foreach (var file in _locator.FindSaveFiles(folder))
                DetectedSaves.Add(new DetectedSaveViewModel(file));

        StatusMessage = DetectedSaves.Count == 0
            ? $"No Bloodborne saves found under {root}."
            : $"Found {DetectedSaves.Count} save(s) under shadPS4.";
    }

    private void RefreshSlots()
    {
        foreach (var slot in Slots)
        {
            try
            {
                var dungeon = _saves.GetSlot(slot.Number);
                slot.Occupied = !dungeon.IsEmpty();
                slot.Summary = slot.Occupied ? "occupied" : "empty";
            }
            catch
            {
                slot.Occupied = false;
                slot.Summary = "—";
            }
        }
    }
}
