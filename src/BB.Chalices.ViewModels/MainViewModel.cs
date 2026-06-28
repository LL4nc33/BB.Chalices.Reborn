using System.Collections.ObjectModel;
using System.Reactive;
using BB.Chalices.Core.Binary;
using BB.Chalices.Services;
using ReactiveUI;

namespace BB.Chalices.ViewModels;

public enum AppView { Catalogue, Settings }

public class MainViewModel : ViewModelBase
{
    private const string AllCategories = "All";

    private readonly SaveFileService _saves;
    private readonly DungeonService _dungeons;
    private readonly SaveLocatorService _locator;
    private readonly ConfigService _config;
    private readonly BackupService _backups;
    private readonly OnlineImportService _online;

    private List<DungeonViewModel> _all = new();
    private bool _suppressEdits;

    private string? _characterName;
    private bool _hasLoadedSave;
    private string _statusMessage = "Welcome. Open a save, or use Detect to find your shadPS4 characters.";
    private string _searchText = string.Empty;
    private string _selectedCategory = AllCategories;
    private DungeonViewModel? _selectedDungeon;
    private SlotViewModel? _selectedSlot;
    private DetectedSaveViewModel? _selectedDetectedSave;

    private string _selectedSlotType = "";
    private bool _poisonPossible;
    private bool _poisonEnabled;
    private bool _fourthLayerPossible;
    private bool _fourthLayerOpen;
    private string _slotHexDump = string.Empty;

    public MainViewModel(SaveFileService saves, DungeonService dungeons, SaveLocatorService locator,
        ConfigService config, BackupService backups, OnlineImportService online)
    {
        _saves = saves;
        _dungeons = dungeons;
        _locator = locator;
        _config = config;
        _backups = backups;
        _online = online;

        Dungeons = new ObservableCollection<DungeonViewModel>();
        Categories = new ObservableCollection<string> { AllCategories };
        Slots = new ObservableCollection<SlotViewModel>(Enumerable.Range(1, 6).Select(n => new SlotViewModel(n)));
        DetectedSaves = new ObservableCollection<DetectedSaveViewModel>();
        RiteSlots = new ObservableCollection<RiteSlotViewModel>(
            Enumerable.Range(0, 4).Select(i => new RiteSlotViewModel(i, ApplyRite)));
        Fields = new ObservableCollection<HeadstoneFieldViewModel>(
            Headstone.Fields.Select(f => new HeadstoneFieldViewModel(f, ApplyField)));
        _selectedSlot = Slots[0];

        LoadDungeonsCommand = ReactiveCommand.CreateFromTask(LoadDungeonsAsync);
        LoadSaveCommand = ReactiveCommand.Create<string>(LoadSave);
        SaveCommand = ReactiveCommand.Create(Save);
        ApplyDungeonCommand = ReactiveCommand.Create(ApplyDungeon);
        DetectSavesCommand = ReactiveCommand.Create(DetectSaves);
        UpdateDungeonsCommand = ReactiveCommand.CreateFromTask(UpdateDungeonsAsync);
        ClearSlotCommand = ReactiveCommand.Create(ClearSlot);
        UndoSlotCommand = ReactiveCommand.Create(UndoSlot);
        RenameCommand = ReactiveCommand.Create(Rename);
    }

    public ObservableCollection<DungeonViewModel> Dungeons { get; }
    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<SlotViewModel> Slots { get; }
    public ObservableCollection<DetectedSaveViewModel> DetectedSaves { get; }
    public ObservableCollection<RiteSlotViewModel> RiteSlots { get; }
    public ObservableCollection<HeadstoneFieldViewModel> Fields { get; }

    public string? CurrentSavePath => _saves.CurrentPath;

    public string? CharacterName
    {
        get => _characterName;
        private set => this.RaiseAndSetIfChanged(ref _characterName, value);
    }

    // The editable copy shown in the rename box; applied to the save on Rename.
    private string? _editableName;
    public string? EditableName
    {
        get => _editableName;
        set => this.RaiseAndSetIfChanged(ref _editableName, value);
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
        set { this.RaiseAndSetIfChanged(ref _searchText, value); ApplyFilter(); }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set { this.RaiseAndSetIfChanged(ref _selectedCategory, value); ApplyFilter(); }
    }

    public DungeonViewModel? SelectedDungeon
    {
        get => _selectedDungeon;
        set => this.RaiseAndSetIfChanged(ref _selectedDungeon, value);
    }

    public SlotViewModel? SelectedSlot
    {
        get => _selectedSlot;
        set { this.RaiseAndSetIfChanged(ref _selectedSlot, value); LoadSelectedSlot(); }
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

    // --- Editor state for the selected slot ---

    public string SelectedSlotType
    {
        get => _selectedSlotType;
        private set => this.RaiseAndSetIfChanged(ref _selectedSlotType, value);
    }

    private string _selectedSlotJoin = string.Empty;
    public string SelectedSlotJoin
    {
        get => _selectedSlotJoin;
        private set => this.RaiseAndSetIfChanged(ref _selectedSlotJoin, value);
    }

    // --- Centre view navigation (catalogue or the inline settings page) ---

    private AppView _currentView = AppView.Catalogue;
    public AppView CurrentView
    {
        get => _currentView;
        set
        {
            this.RaiseAndSetIfChanged(ref _currentView, value);
            this.RaisePropertyChanged(nameof(IsCatalogueView));
            this.RaisePropertyChanged(nameof(IsSettingsView));
        }
    }

    public bool IsCatalogueView => CurrentView == AppView.Catalogue;
    public bool IsSettingsView => CurrentView == AppView.Settings;

    private string _shadPs4Path = string.Empty;
    public string ShadPs4Path
    {
        get => _shadPs4Path;
        set => this.RaiseAndSetIfChanged(ref _shadPs4Path, value);
    }

    private string _backupPath = string.Empty;
    public string BackupPath
    {
        get => _backupPath;
        set => this.RaiseAndSetIfChanged(ref _backupPath, value);
    }

    private bool _autoBackup;
    public bool AutoBackup
    {
        get => _autoBackup;
        set => this.RaiseAndSetIfChanged(ref _autoBackup, value);
    }

    // Load the current settings into the form and switch to the settings page.
    public void OpenSettings()
    {
        ShadPs4Path = _config.Settings.ShadPs4FolderPath ?? string.Empty;
        BackupPath = _config.BackupDirectory;
        AutoBackup = _config.Settings.AutoBackupEnabled;
        CurrentView = AppView.Settings;
    }

    public void SaveSettings()
    {
        _config.Settings.ShadPs4FolderPath = string.IsNullOrWhiteSpace(ShadPs4Path) ? null : ShadPs4Path;
        _config.Settings.BackupDirectory = string.IsNullOrWhiteSpace(BackupPath) ? null : BackupPath;
        _config.Settings.AutoBackupEnabled = AutoBackup;
        _config.Save();
        CurrentView = AppView.Catalogue;
    }

    public bool PoisonPossible
    {
        get => _poisonPossible;
        private set => this.RaiseAndSetIfChanged(ref _poisonPossible, value);
    }

    public bool PoisonEnabled
    {
        get => _poisonEnabled;
        set { this.RaiseAndSetIfChanged(ref _poisonEnabled, value); ApplyPoison(value); }
    }

    public bool FourthLayerPossible
    {
        get => _fourthLayerPossible;
        private set => this.RaiseAndSetIfChanged(ref _fourthLayerPossible, value);
    }

    public bool FourthLayerOpen
    {
        get => _fourthLayerOpen;
        set { this.RaiseAndSetIfChanged(ref _fourthLayerOpen, value); ApplyFourthLayer(value); }
    }

    public string SlotHexDump
    {
        get => _slotHexDump;
        private set => this.RaiseAndSetIfChanged(ref _slotHexDump, value);
    }

    public ReactiveCommand<Unit, Unit> LoadDungeonsCommand { get; }
    public ReactiveCommand<string, Unit> LoadSaveCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> ApplyDungeonCommand { get; }
    public ReactiveCommand<Unit, Unit> DetectSavesCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateDungeonsCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearSlotCommand { get; }
    public ReactiveCommand<Unit, Unit> UndoSlotCommand { get; }
    public ReactiveCommand<Unit, Unit> RenameCommand { get; }

    private bool _canUndo;
    public bool CanUndo
    {
        get => _canUndo;
        private set => this.RaiseAndSetIfChanged(ref _canUndo, value);
    }

    private async Task LoadDungeonsAsync()
    {
        var all = await _dungeons.GetAllAsync();
        _all = all.Select(d => new DungeonViewModel(d)).ToList();
        RebuildCategories();
        StatusMessage = $"{_all.Count} dungeons ready.";
    }

    // Noxde's curated list is kept separate from the full Tomb Prospectors set.
    private static readonly HashSet<string> NoxCategories =
        new(StringComparer.OrdinalIgnoreCase) { "farming", "equipment", "bloodgems", "testing" };

    private bool _showNox = true;
    public bool ShowNox
    {
        get => _showNox;
        set
        {
            this.RaiseAndSetIfChanged(ref _showNox, value);
            this.RaisePropertyChanged(nameof(ShowAll));
            RebuildCategories();
        }
    }

    public bool ShowAll => !_showNox;

    private void RebuildCategories()
    {
        Categories.Clear();
        Categories.Add(AllCategories);
        foreach (var category in _all
            .Where(d => NoxCategories.Contains(d.Category) == _showNox)
            .Select(d => d.Category).Distinct().OrderBy(c => c))
            Categories.Add(category);

        SelectedCategory = AllCategories; // also triggers ApplyFilter
    }

    private async Task UpdateDungeonsAsync()
    {
        StatusMessage = "Fetching the latest dungeons…";
        var result = await _online.UpdateAsync();
        await LoadDungeonsAsync();
        StatusMessage = result;
    }

    private void ApplyFilter()
    {
        IEnumerable<DungeonViewModel> query = _all
            .Where(d => NoxCategories.Contains(d.Category) == _showNox);

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
            EditableName = save.CharacterName;
            HasLoadedSave = true;
            _config.Settings.LastSavePath = path;
            _config.Save();
            RefreshSlots();
            _undoSlot = null;
            _undoBytes = null;
            CanUndo = false;
            LoadSelectedSlot();
            StatusMessage = $"Loaded {System.IO.Path.GetFileName(path)} (Hunter {save.CharacterName}).";
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
            if (_config.Settings.AutoBackupEnabled && _saves.CurrentPath is { } path)
                _backups.Create(path, "before save");

            _saves.Save();
            StatusMessage = "Saved. A backup was written first.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    private void ApplyDungeon()
    {
        if (!HasLoadedSave) { StatusMessage = "Open a save first."; return; }
        if (SelectedDungeon is null) { StatusMessage = "Pick a dungeon from the list first."; return; }
        if (SelectedSlot is null) { StatusMessage = "Pick a slot first."; return; }

        CaptureUndo();
        _saves.SetSlot(SelectedSlot.Number, SelectedDungeon.Bytes);
        RefreshSlot(SelectedSlot);
        LoadSelectedSlot();
        StatusMessage = $"Placed {SelectedDungeon.Glyph} in slot {SelectedSlot.Number}. Save to write it to disk.";
    }

    private void ClearSlot()
    {
        if (!HasLoadedSave || SelectedSlot is null)
        {
            StatusMessage = "Open a save and pick a slot first.";
            return;
        }

        CaptureUndo();
        _saves.ClearSlot(SelectedSlot.Number);
        RefreshSlot(SelectedSlot);
        LoadSelectedSlot();
        StatusMessage = $"Cleared slot {SelectedSlot.Number}. Save to write it to disk.";
    }

    public void PlaceBuiltDungeon(byte[]? record)
    {
        if (record is null)
        {
            StatusMessage = "Could not build that dungeon.";
            return;
        }
        if (!HasLoadedSave || SelectedSlot is null)
        {
            StatusMessage = "Open a save and pick a slot first.";
            return;
        }

        CaptureUndo();
        _saves.SetSlot(SelectedSlot.Number, record);
        RefreshSlot(SelectedSlot);
        LoadSelectedSlot();
        StatusMessage = $"Built dungeon placed in slot {SelectedSlot.Number}. Save to write it to disk.";
    }

    private int? _undoSlot;
    private byte[]? _undoBytes;

    // Snapshot the current slot before a destructive write so it can be reverted.
    private void CaptureUndo()
    {
        if (SelectedSlot is null)
            return;
        _undoSlot = SelectedSlot.Number;
        _undoBytes = _saves.GetSlotBytes(SelectedSlot.Number);
        CanUndo = true;
    }

    private void UndoSlot()
    {
        if (_undoSlot is null || _undoBytes is null)
            return;

        _saves.SetSlot(_undoSlot.Value, _undoBytes);
        SlotViewModel? slot = Slots.FirstOrDefault(s => s.Number == _undoSlot.Value);
        if (slot is not null)
        {
            RefreshSlot(slot);
            if (slot == SelectedSlot)
                LoadSelectedSlot();
        }
        StatusMessage = $"Reverted slot {_undoSlot.Value}. Save to write it to disk.";
        _undoSlot = null;
        _undoBytes = null;
        CanUndo = false;
    }

    private void Rename()
    {
        if (!HasLoadedSave)
            return;

        string name = (EditableName ?? string.Empty).Trim();
        if (name.Length is < 1 or > 16)
        {
            StatusMessage = "The hunter name must be 1 to 16 characters.";
            return;
        }

        _saves.SetCharacterName(name);
        CharacterName = name;
        EditableName = name;
        StatusMessage = $"Renamed to {name}. Click Save changes to write it.";
    }

    private void DetectSaves()
    {
        DetectedSaves.Clear();

        var configured = _config.Settings.ShadPs4FolderPath;
        var root = !string.IsNullOrWhiteSpace(configured) && System.IO.Directory.Exists(configured)
            ? configured
            : _locator.GuessShadPs4Root();
        if (root is null)
        {
            StatusMessage = "Couldn't find a shadPS4 folder. Set it in Settings or use Open Save.";
            return;
        }

        foreach (var folder in _locator.FindSaveFolders(root))
            foreach (var file in _locator.FindSaveFiles(folder))
                DetectedSaves.Add(new DetectedSaveViewModel(file, SaveFileService.PeekCharacterName(file)));

        StatusMessage = DetectedSaves.Count == 0
            ? $"No Bloodborne saves found under {root}. Use Open Save, or set the folder in Settings."
            : $"Found {DetectedSaves.Count} character save(s). Pick one above to start editing.";
    }

    // --- Editing the selected slot's headstone fields ---

    private void ApplyRite(int index, Headstone.Rite rite)
    {
        if (_suppressEdits || !HasLoadedSave || SelectedSlot is null)
            return;

        _saves.SetRite(SelectedSlot.Number, index, rite);
        AfterEdit($"Rite {index + 1}: {rite}.");
    }

    private void ApplyPoison(bool enabled)
    {
        if (_suppressEdits || !HasLoadedSave || SelectedSlot is null || !PoisonPossible)
            return;

        _saves.SetPoison(SelectedSlot.Number, enabled);
        AfterEdit(enabled ? "Poison on." : "Poison off.");
    }

    private void ApplyFourthLayer(bool open)
    {
        if (_suppressEdits || !HasLoadedSave || SelectedSlot is null || !FourthLayerPossible)
            return;

        _saves.SetFourthLayer(SelectedSlot.Number, open);
        AfterEdit(open ? "4th layer opened." : "4th layer closed.");
    }

    private void ApplyField(Headstone.HeadstoneField field, string hex)
    {
        if (_suppressEdits || !HasLoadedSave || SelectedSlot is null)
            return;
        if (!_saves.TrySetField(SelectedSlot.Number, field, hex))
            return;

        SlotHexDump = _saves.SlotHexDump(SelectedSlot.Number);
        RefreshSlot(SelectedSlot);
        StatusMessage = $"{field.Name} set. Save to write it to disk.";
    }

    private void AfterEdit(string message)
    {
        if (SelectedSlot is null)
            return;

        SlotHexDump = _saves.SlotHexDump(SelectedSlot.Number);
        RefreshSlot(SelectedSlot);
        StatusMessage = $"{message} Save to write it to disk.";
    }

    private void LoadSelectedSlot()
    {
        _suppressEdits = true;
        try
        {
            if (!HasLoadedSave || SelectedSlot is null)
            {
                SelectedSlotType = "";
                SelectedSlotJoin = "";
                foreach (var rite in RiteSlots) rite.Set(Headstone.Rite.None);
                PoisonPossible = FourthLayerPossible = false;
                PoisonEnabled = FourthLayerOpen = false;
                foreach (var field in Fields) field.Set(string.Empty);
                SlotHexDump = string.Empty;
                return;
            }

            var record = _saves.GetSlotBytes(SelectedSlot.Number);
            SlotHexDump = _saves.SlotHexDump(SelectedSlot.Number);
            for (int i = 0; i < Fields.Count; i++)
                Fields[i].Set(Headstone.ReadFieldHex(record, Fields[i].Field));

            if (IsEmpty(record))
            {
                SelectedSlotType = "empty";
                SelectedSlotJoin = "";
                foreach (var rite in RiteSlots) rite.Set(Headstone.Rite.None);
                PoisonPossible = FourthLayerPossible = false;
                PoisonEnabled = FourthLayerOpen = false;
                return;
            }

            SelectedSlotType = Headstone.DungeonType(record);
            SelectedSlotJoin = Headstone.JoinRequirementsLabel(Headstone.JoinRequirementsHex(record));
            for (int i = 0; i < RiteSlots.Count; i++)
                RiteSlots[i].Set(Headstone.ReadRite(record, Headstone.RiteSlotOffsets[i]));

            PoisonPossible = Headstone.PoisonPossible(record);
            PoisonEnabled = Headstone.IsPoisoned(record);
            FourthLayerPossible = Headstone.FourthLayerPossible(record);
            FourthLayerOpen = Headstone.IsFourthLayerOpen(record);
        }
        finally
        {
            _suppressEdits = false;
        }
    }

    private void RefreshSlots()
    {
        foreach (var slot in Slots)
            RefreshSlot(slot);
    }

    private void RefreshSlot(SlotViewModel slot)
    {
        try
        {
            var record = _saves.GetSlotBytes(slot.Number);
            if (IsEmpty(record))
            {
                slot.Occupied = false;
                slot.Headline = "empty";
                return;
            }

            slot.Occupied = true;
            slot.Headline = Headstone.DungeonType(record);
        }
        catch
        {
            slot.Occupied = false;
            slot.Headline = "?";
        }
    }

    private static bool IsEmpty(byte[] record) =>
        record.Length >= 4 && record[0] == 0xFF && record[1] == 0xFF && record[2] == 0xFF && record[3] == 0xFF;
}
