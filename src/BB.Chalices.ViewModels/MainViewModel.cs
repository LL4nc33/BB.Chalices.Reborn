using System.Collections.ObjectModel;
using System.Reactive;
using BB.Chalices.Core.Binary;
using BB.Chalices.Core.Saves;
using BB.Chalices.Core.Sharing;
using BB.Chalices.Data.Entities;
using BB.Chalices.Services;
using ReactiveUI;

namespace BB.Chalices.ViewModels;

public enum AppView { Catalogue, Settings, Backups }

public class MainViewModel : ViewModelBase
{
    private const string AllCategories = "All";

    private readonly SaveFileService _saves;
    private readonly DungeonService _dungeons;
    private readonly SaveLocatorService _locator;
    private readonly ConfigService _config;
    private readonly BackupService _backups;
    private readonly OnlineImportService _online;
    private readonly ListService _lists;

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
        ConfigService config, BackupService backups, OnlineImportService online, ListService lists)
    {
        _saves = saves;
        _dungeons = dungeons;
        _locator = locator;
        _config = config;
        _backups = backups;
        _online = online;
        _lists = lists;
        Builder = new DungeonBuilderViewModel(dungeons);

        Dungeons = new ObservableCollection<DungeonViewModel>();
        Lists = new ObservableCollection<DungeonList>();
        Categories = new ObservableCollection<string> { AllCategories };
        // Slot 0 = the makeshift altar, then the six stored slots 1-6.
        Slots = new ObservableCollection<SlotViewModel>(Enumerable.Range(0, 7).Select(n => new SlotViewModel(n)));
        DetectedSaves = new ObservableCollection<DetectedSaveViewModel>();
        RiteSlots = new ObservableCollection<RiteSlotViewModel>(
            Enumerable.Range(0, 4).Select(i => new RiteSlotViewModel(i, ApplyRite)));
        Fields = new ObservableCollection<HeadstoneFieldViewModel>(
            Headstone.Fields.Select(f => new HeadstoneFieldViewModel(f, ApplyField)));
        _selectedSlot = Slots.First(s => s.Number == 1);

        LoadDungeonsCommand = ReactiveCommand.CreateFromTask(LoadDungeonsAsync);
        LoadSaveCommand = ReactiveCommand.Create<string>(LoadSave);
        SaveCommand = ReactiveCommand.Create(Save);
        ApplyDungeonCommand = ReactiveCommand.Create(ApplyDungeon);
        FillAllSlotsCommand = ReactiveCommand.Create(FillAllSlots);
        DetectSavesCommand = ReactiveCommand.Create(DetectSaves);
        UpdateDungeonsCommand = ReactiveCommand.CreateFromTask(UpdateDungeonsAsync);
        ClearSlotCommand = ReactiveCommand.Create(ClearSlot);
        UndoSlotCommand = ReactiveCommand.Create(UndoSlot);
        CreateBackupCommand = ReactiveCommand.Create(CreateBackup);
        RestoreBackupCommand = ReactiveCommand.Create(RestoreSelectedBackup);
        ApplySoundFixCommand = ReactiveCommand.Create(ApplySoundFix);
        LaunchShadPs4Command = ReactiveCommand.Create(LaunchShadPs4);
        ZoomInCommand = ReactiveCommand.Create(() => Adjust(ZoomStep));
        ZoomOutCommand = ReactiveCommand.Create(() => Adjust(-ZoomStep));

        // Route any unhandled command error to the status bar instead of letting it
        // bubble out of the command and crash the app.
        foreach (var command in new IReactiveCommand[]
        {
            LoadDungeonsCommand, LoadSaveCommand, SaveCommand, ApplyDungeonCommand,
            FillAllSlotsCommand, DetectSavesCommand, UpdateDungeonsCommand, ClearSlotCommand,
            UndoSlotCommand, CreateBackupCommand, RestoreBackupCommand, ApplySoundFixCommand,
            LaunchShadPs4Command, ZoomInCommand, ZoomOutCommand,
        })
            command.ThrownExceptions.Subscribe(ex => StatusMessage = $"Something went wrong: {ex.Message}");

        _sidebarFactor = InitFactor(_config.Settings.SidebarZoom);
        _catalogueFactor = InitFactor(_config.Settings.CatalogueZoom);
        _editorFactor = InitFactor(_config.Settings.EditorZoom);

        _middleZoomOption = new ZoomTargetOption(ZoomTarget.Catalogue, "Catalogue");
        ZoomTargets = new ObservableCollection<ZoomTargetOption>
        {
            new(ZoomTarget.All, "All"),
            new(ZoomTarget.Sidebar, "Sidebar"),
            _middleZoomOption,
            new(ZoomTarget.Editor, "Editor"),
        };
        _selectedZoomOption = ZoomTargets[0];
    }

    private static double InitFactor(double factor) =>
        factor is >= MinFactor and <= MaxFactor ? factor : 1.0;

    // --- UI zoom (the +/- buttons) ---
    // The displayed percentage is a factor (100% = the column's default size). Each
    // column has its own baseline, so "All at 100%" means sidebar 1.2x, catalogue and
    // editor 1.3x. The actual render scale is baseline * factor.
    private const double ZoomStep = 0.1;
    private const double MinFactor = 0.8;
    private const double MaxFactor = 2.0;
    private const double SidebarBaseline = 1.2;
    private const double CatalogueBaseline = 1.3;
    private const double EditorBaseline = 1.3;

    // Each of the three main columns has its own zoom factor, so the +/- buttons can
    // target one column at a time (or all at once). The bound Scale is baseline * factor.
    private double _sidebarFactor = 1.0;
    public double SidebarScale => SidebarBaseline * _sidebarFactor;

    private double _catalogueFactor = 1.0;
    public double CatalogueScale => CatalogueBaseline * _catalogueFactor;

    private double _editorFactor = 1.0;
    public double EditorScale => EditorBaseline * _editorFactor;

    public ObservableCollection<ZoomTargetOption> ZoomTargets { get; }

    // The middle option, whose label tracks the active view.
    private readonly ZoomTargetOption _middleZoomOption;

    private ZoomTargetOption _selectedZoomOption = null!;
    public ZoomTargetOption SelectedZoomTargetOption
    {
        get => _selectedZoomOption;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedZoomOption, value);
            this.RaisePropertyChanged(nameof(UiScalePercent));
        }
    }

    public ZoomTarget SelectedZoomTarget => _selectedZoomOption?.Target ?? ZoomTarget.All;

    private void UpdateMiddleZoomLabel() =>
        _middleZoomOption?.SetLabel(CurrentView switch
        {
            AppView.Settings => "Settings",
            AppView.Backups => "Backups",
            _ => "Catalogue",
        });

    // Clicking inside a column points the +/- buttons at it.
    public void SelectZoomTarget(ZoomTarget target)
    {
        var option = ZoomTargets.FirstOrDefault(o => o.Target == target);
        if (option is not null && !ReferenceEquals(option, SelectedZoomTargetOption))
            SelectedZoomTargetOption = option;
    }

    public string UiScalePercent => $"{FactorFor(SelectedZoomTarget) * 100:0}%";

    private double FactorFor(ZoomTarget target) => target switch
    {
        ZoomTarget.Sidebar => _sidebarFactor,
        ZoomTarget.Editor => _editorFactor,
        _ => _catalogueFactor, // Catalogue and All display the catalogue factor
    };

    // The app version shown under the logo (from the assembly version).
    public string AppVersion
    {
        get
        {
            var v = (System.Reflection.Assembly.GetEntryAssembly() ?? typeof(MainViewModel).Assembly).GetName().Version;
            return v is null ? "" : $"v{v.Major}.{v.Minor}.{v.Build}";
        }
    }

    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }

    // Grow or shrink the selected column (or all three) by one step, then persist.
    private void Adjust(double delta)
    {
        double value = Math.Clamp(Math.Round(FactorFor(SelectedZoomTarget) + delta, 2), MinFactor, MaxFactor);
        switch (SelectedZoomTarget)
        {
            case ZoomTarget.Sidebar:
                _sidebarFactor = value;
                this.RaisePropertyChanged(nameof(SidebarScale));
                break;
            case ZoomTarget.Editor:
                _editorFactor = value;
                this.RaisePropertyChanged(nameof(EditorScale));
                break;
            case ZoomTarget.Catalogue:
                _catalogueFactor = value;
                this.RaisePropertyChanged(nameof(CatalogueScale));
                break;
            default: // All
                _sidebarFactor = _catalogueFactor = _editorFactor = value;
                this.RaisePropertyChanged(nameof(SidebarScale));
                this.RaisePropertyChanged(nameof(CatalogueScale));
                this.RaisePropertyChanged(nameof(EditorScale));
                break;
        }

        _config.Settings.SidebarZoom = _sidebarFactor;
        _config.Settings.CatalogueZoom = _catalogueFactor;
        _config.Settings.EditorZoom = _editorFactor;
        _config.Save();
        this.RaisePropertyChanged(nameof(UiScalePercent));
    }

    public ObservableCollection<DungeonViewModel> Dungeons { get; }
    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<SlotViewModel> Slots { get; }
    public ObservableCollection<DetectedSaveViewModel> DetectedSaves { get; }
    public ObservableCollection<RiteSlotViewModel> RiteSlots { get; }
    public ObservableCollection<HeadstoneFieldViewModel> Fields { get; }

    // Warns about rite combinations that misbehave (from the Tomb Prospectors research).
    public string? RiteWarning => RiteWarnings.For(RiteSlots.Select(r => r.Rite));

    // The 8-byte-and-shorter fields go in two columns; the 16-byte creator and
    // character-name fields each get a full-width row of their own.
    public IReadOnlyList<HeadstoneFieldViewModel> ShortFields => Fields.Where(f => f.Field.Length <= 8).ToList();
    public IReadOnlyList<HeadstoneFieldViewModel> LongFields => Fields.Where(f => f.Field.Length > 8).ToList();

    public string? CharacterName
    {
        get => _characterName;
        private set => this.RaiseAndSetIfChanged(ref _characterName, value);
    }

    // The editable copies shown in the character boxes; applied to the save on Save.
    private string? _editableName;
    public string? EditableName
    {
        get => _editableName;
        set => this.RaiseAndSetIfChanged(ref _editableName, value);
    }

    private string _editableInsight = string.Empty;
    public string EditableInsight
    {
        get => _editableInsight;
        set => this.RaiseAndSetIfChanged(ref _editableInsight, value);
    }

    private string _editableEchoes = string.Empty;
    public string EditableEchoes
    {
        get => _editableEchoes;
        set => this.RaiseAndSetIfChanged(ref _editableEchoes, value);
    }

    private string _editableLevel = string.Empty;
    public string EditableLevel
    {
        get => _editableLevel;
        set => this.RaiseAndSetIfChanged(ref _editableLevel, value);
    }

    public bool HasLoadedSave
    {
        get => _hasLoadedSave;
        private set
        {
            this.RaiseAndSetIfChanged(ref _hasLoadedSave, value);
            this.RaisePropertyChanged(nameof(CanPlaceDungeon));
        }
    }

    // Gate the primary buttons so they aren't clickable when they'd only print a hint.
    public bool CanPlaceDungeon => HasLoadedSave && HasSelectedDungeon;

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
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedDungeon, value);
            this.RaisePropertyChanged(nameof(HasSelectedDungeon));
            this.RaisePropertyChanged(nameof(CanPlaceDungeon));
        }
    }

    // Gates the place/fill buttons so they aren't clickable when they'd just no-op.
    public bool HasSelectedDungeon => _selectedDungeon is not null;

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
            this.RaisePropertyChanged(nameof(IsBackupsView));
            UpdateMiddleZoomLabel();
        }
    }

    public bool IsCatalogueView => CurrentView == AppView.Catalogue;
    public bool IsSettingsView => CurrentView == AppView.Settings;
    public bool IsBackupsView => CurrentView == AppView.Backups;

    private string _shadPs4Path = string.Empty;
    public string ShadPs4Path
    {
        get => _shadPs4Path;
        set { this.RaiseAndSetIfChanged(ref _shadPs4Path, value); this.RaisePropertyChanged(nameof(DetectedShadInfo)); }
    }

    // A hint under the shadPS4 path field: what auto-detect would use.
    public string DetectedShadInfo
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ShadPs4Path))
                return System.IO.Directory.Exists(ShadPs4Path)
                    ? "Using the folder above."
                    : "That folder does not exist - fix the path, or clear it to auto-detect.";
            string? guess = _locator.GuessShadPs4Root();
            return guess is null
                ? "No shadPS4 folder found automatically. Set it above, or just use Open save."
                : $"Auto-detected: {guess}";
        }
    }

    private string _shadPs4Exe = string.Empty;
    // The exact shadPS4 program to launch, set in Settings. Overrides the search
    // near the save folder when the build's name or location is unusual.
    public string ShadPs4Exe
    {
        get => _shadPs4Exe;
        set => this.RaiseAndSetIfChanged(ref _shadPs4Exe, value);
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

    // Where the app keeps its settings, database and catalogue cache. This lives in
    // a data/ folder next to the app (portable), so moving the app folder takes the
    // data with it. Read-only: there's nothing to configure.
    public string DataFolder => _config.DataDirectory;
    public string StorageModeText => AppPaths.IsPortable
        ? "Data is kept in a data/ folder next to the app, so moving the app folder takes your saves-list, backups and settings with it."
        : "The app folder isn't writable, so data is kept in your user profile instead.";

    // Load the current settings into the form and switch to the settings page.
    public void OpenSettings()
    {
        ShadPs4Path = _config.Settings.ShadPs4FolderPath ?? string.Empty;
        ShadPs4Exe = _config.Settings.ShadPs4ExePath ?? string.Empty;
        CurrentView = AppView.Settings;
    }

    public void SaveSettings()
    {
        _config.Settings.ShadPs4FolderPath = string.IsNullOrWhiteSpace(ShadPs4Path) ? null : ShadPs4Path;
        _config.Settings.ShadPs4ExePath = string.IsNullOrWhiteSpace(ShadPs4Exe) ? null : ShadPs4Exe;
        _config.Save();
        CurrentView = AppView.Catalogue;
    }

    // --- Backups (inline view, recycled from the old BackupWindow) ---

    public ObservableCollection<BackupInfo> BackupItems { get; } = new();

    private BackupInfo? _selectedBackup;
    public BackupInfo? SelectedBackup
    {
        get => _selectedBackup;
        set => this.RaiseAndSetIfChanged(ref _selectedBackup, value);
    }

    public bool HasBackups => BackupItems.Count > 0;

    // Count and total size of the kept backups, for the Backups header.
    public string BackupSummary
    {
        get
        {
            if (BackupItems.Count == 0)
                return "";
            long total = 0;
            foreach (var backup in BackupItems)
                try { total += new System.IO.FileInfo(backup.FilePath).Length; }
                catch { /* a backup file may have been removed under us */ }
            return $"{BackupItems.Count} backup(s)  ·  {total / 1024.0 / 1024.0:0.0} MB";
        }
    }

    public void OpenBackups()
    {
        BackupPath = _config.BackupDirectory;
        AutoBackup = _config.Settings.AutoBackupEnabled;
        RefreshBackups();
        CurrentView = AppView.Backups;
    }

    // The backup folder and auto-backup toggle now live in the Backups view, so they
    // persist as soon as you change them rather than via a Save button.
    public void PersistBackupPath()
    {
        _config.Settings.BackupDirectory = string.IsNullOrWhiteSpace(BackupPath) ? null : BackupPath;
        _config.Save();
    }

    public void PersistAutoBackup()
    {
        _config.Settings.AutoBackupEnabled = AutoBackup;
        _config.Save();
    }

    private void RefreshBackups()
    {
        BackupItems.Clear();
        foreach (var backup in _backups.GetAll())
            BackupItems.Add(backup);
        this.RaisePropertyChanged(nameof(HasBackups));
        this.RaisePropertyChanged(nameof(BackupSummary));
    }

    private void CreateBackup()
    {
        if (_saves.CurrentPath is { } path)
            StatusMessage = _backups.Create(path, "manual");
        RefreshBackups();
    }

    private void RestoreSelectedBackup()
    {
        if (_saves.CurrentPath is not { } path || SelectedBackup is null)
            return;

        StatusMessage = _backups.Restore(path, SelectedBackup);
        LoadSave(path); // the file on disk changed, reload it into the editor
        RefreshBackups();
    }

    // Deletion is confirmed in the view; this just removes the selected backup.
    public void DeleteSelectedBackup()
    {
        if (SelectedBackup is null)
            return;

        StatusMessage = _backups.Delete(SelectedBackup);
        RefreshBackups();
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

    private bool _difficultyPossible;
    public bool DifficultyPossible
    {
        get => _difficultyPossible;
        private set => this.RaiseAndSetIfChanged(ref _difficultyPossible, value);
    }

    private bool _difficultyUp;
    public bool DifficultyUp
    {
        get => _difficultyUp;
        set { this.RaiseAndSetIfChanged(ref _difficultyUp, value); ApplyDifficulty(value); }
    }

    public ObservableCollection<Headstone.SpecialEnemy> SpecialEnemyOptions { get; } = new();

    private Headstone.SpecialEnemy? _selectedSpecialEnemy;
    public Headstone.SpecialEnemy? SelectedSpecialEnemy
    {
        get => _selectedSpecialEnemy;
        set { this.RaiseAndSetIfChanged(ref _selectedSpecialEnemy, value); ApplySpecialEnemy(value); }
    }

    public string SlotHexDump
    {
        get => _slotHexDump;
        private set => this.RaiseAndSetIfChanged(ref _slotHexDump, value);
    }

    // The selected slot's 125 bytes, for the colour-coded live-bytes view.
    private byte[]? _selectedSlotBytes;
    public byte[]? SelectedSlotBytes
    {
        get => _selectedSlotBytes;
        private set => this.RaiseAndSetIfChanged(ref _selectedSlotBytes, value);
    }

    // The last-saved bytes per slot, indexed by slot number 0-6 (0 = makeshift);
    // the live view colours only what differs from these.
    private readonly byte[]?[] _slotBaselines = new byte[7][];

    private byte[]? _selectedSlotBaseline;
    public byte[]? SelectedSlotBaseline
    {
        get => _selectedSlotBaseline;
        private set => this.RaiseAndSetIfChanged(ref _selectedSlotBaseline, value);
    }

    // The selected slot's absolute file offset, so the live view shows real offsets.
    private int _selectedSlotOffset;
    public int SelectedSlotOffset
    {
        get => _selectedSlotOffset;
        private set => this.RaiseAndSetIfChanged(ref _selectedSlotOffset, value);
    }

    public ReactiveCommand<Unit, Unit> LoadDungeonsCommand { get; }
    public ReactiveCommand<string, Unit> LoadSaveCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> ApplyDungeonCommand { get; }
    public ReactiveCommand<Unit, Unit> FillAllSlotsCommand { get; }
    public ReactiveCommand<Unit, Unit> DetectSavesCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateDungeonsCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearSlotCommand { get; }
    public ReactiveCommand<Unit, Unit> UndoSlotCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateBackupCommand { get; }
    public ReactiveCommand<Unit, Unit> RestoreBackupCommand { get; }
    public ReactiveCommand<Unit, Unit> ApplySoundFixCommand { get; }
    public ReactiveCommand<Unit, Unit> LaunchShadPs4Command { get; }

    private bool _soundFixNeeded;
    // True when a shadPS4 save folder still needs the sound-crash workaround,
    // which drives the auto-shown fix button in the sidebar.
    public bool SoundFixNeeded
    {
        get => _soundFixNeeded;
        private set => this.RaiseAndSetIfChanged(ref _soundFixNeeded, value);
    }

    private bool _canUndo;
    public bool CanUndo
    {
        get => _canUndo;
        private set => this.RaiseAndSetIfChanged(ref _canUndo, value);
    }

    // The lists shown in the catalogue picker: built-in Nox and Community, plus the
    // user's own. Everything is a list now; there is no separate catalogue.
    public ObservableCollection<DungeonList> Lists { get; }

    // The dungeon builder, shown inline in the middle column (the editor stays on the
    // right) instead of a separate window.
    public DungeonBuilderViewModel Builder { get; }

    private bool _showBuilder;
    public bool ShowBuilder
    {
        get => _showBuilder;
        private set => this.RaiseAndSetIfChanged(ref _showBuilder, value);
    }

    public void OpenBuilder() => ShowBuilder = true;
    public void CloseBuilder() => ShowBuilder = false;

    // Write the built dungeon into a chosen altar slot (selecting it so the editor
    // jumps there). The builder stays open so you can roll and place another.
    public void PlaceBuiltInSlot(int slotNumber)
    {
        if (Builder.Record is not { } record)
        {
            StatusMessage = "Build a dungeon first.";
            return;
        }
        var slot = Slots.FirstOrDefault(s => s.Number == slotNumber);
        if (slot is not null)
            SelectedSlot = slot;
        PlaceBuiltDungeon(record);
    }

    public async Task SaveBuiltAsCustomAsync(string name)
    {
        if (Builder.Record is not { } record)
        {
            StatusMessage = "Build a dungeon first.";
            return;
        }
        int listId = await EnsureMyDungeonsListId();
        await _lists.AddNewDungeonAsync(listId, name, null, record);
        await LoadDungeonsAsync();
        StatusMessage = $"Saved \"{name}\" to My dungeons.";
    }

    public async Task AddBuiltToListAsync(int listId)
    {
        if (Builder.Record is not { } record)
        {
            StatusMessage = "Build a dungeon first.";
            return;
        }
        await _lists.AddNewDungeonAsync(listId, BuiltDungeonName(), null, record);
        await LoadListsAsync();
        StatusMessage = "Added the built dungeon to the list.";
    }

    public async Task CreateListAndAddBuiltAsync(string name)
    {
        if (Builder.Record is not { } record)
        {
            StatusMessage = "Build a dungeon first.";
            return;
        }
        var list = await _lists.CreateListAsync(name);
        await _lists.AddNewDungeonAsync(list.Id, BuiltDungeonName(), null, record);
        await LoadListsAsync();
        StatusMessage = $"Created \"{name}\" and added the built dungeon.";
    }

    private string BuiltDungeonName() => $"{Builder.SelectedArea.Name} layout {Builder.DungeonNumber}";

    private DungeonList? _selectedList;
    public DungeonList? SelectedList
    {
        get => _selectedList;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedList, value);
            this.RaisePropertyChanged(nameof(ShowNoxDownloadPrompt));
            this.RaisePropertyChanged(nameof(ShowEmptyListHint));
            this.RaisePropertyChanged(nameof(CanEditSelectedList));
            this.RaisePropertyChanged(nameof(SelectedListHasItems));
            RebuildCategories();
        }
    }

    // Built-in lists (Nox, Community) can't be edited; only the user's own can.
    public bool CanEditSelectedList => SelectedList?.Source == ListSource.User;

    public bool SelectedListHasItems => SelectedList is { } list && list.Items.Count > 0;

    // The user's own lists, for the "add to list" menu.
    public IEnumerable<DungeonList> UserLists => Lists.Where(l => l.Source == ListSource.User);

    // Show the "download Nox's dungeons" prompt when the empty Nox list is selected.
    public bool ShowNoxDownloadPrompt => SelectedList?.Source == ListSource.Nox && SelectedList.Items.Count == 0;

    // A gentle hint for any other empty list (e.g. a list you just created), telling
    // the user how to fill it. Not shown for the empty Nox list (that has its own prompt).
    public bool ShowEmptyListHint =>
        SelectedList is { } list && list.Items.Count == 0 && list.Source != ListSource.Nox;

    // The dungeons that belong to the selected list (matched by catalogue id).
    private IEnumerable<DungeonViewModel> CurrentListDungeons()
    {
        if (SelectedList is null)
            return Array.Empty<DungeonViewModel>();
        var ids = SelectedList.Items.Select(i => i.DungeonId).ToHashSet();
        return _all.Where(d => ids.Contains(d.Id));
    }

    private async Task LoadDungeonsAsync()
    {
        var all = await _dungeons.GetAllAsync();
        _all = all.Select(d => new DungeonViewModel(d)).ToList();
        await LoadListsAsync();
        await Builder.InitAsync();
        StatusMessage = $"{_all.Count} dungeons ready.";
    }

    // (Re)load the lists, keeping the current selection when possible and defaulting
    // to the always-present Community list.
    public async Task LoadListsAsync()
    {
        int? previous = SelectedList?.Id;
        Lists.Clear();
        foreach (var list in await _lists.GetListsAsync())
            Lists.Add(list);

        SelectedList = Lists.FirstOrDefault(l => l.Id == previous)
            ?? Lists.FirstOrDefault(l => l.Source == ListSource.Bundled)
            ?? Lists.FirstOrDefault();
        this.RaisePropertyChanged(nameof(UserLists));
    }

    private void RebuildCategories()
    {
        Categories.Clear();
        Categories.Add(AllCategories);
        foreach (var category in CurrentListDungeons()
            .Select(d => d.Category).Distinct().OrderBy(c => c))
            Categories.Add(category);

        SelectedCategory = AllCategories; // also triggers ApplyFilter
    }

    private async Task UpdateDungeonsAsync()
    {
        StatusMessage = "Fetching the latest dungeons…";
        var result = await _online.UpdateAsync();
        await _lists.RebuildBuiltInListsAsync();
        await LoadDungeonsAsync();
        StatusMessage = result;
    }

    private void ApplyFilter()
    {
        IEnumerable<DungeonViewModel> query = CurrentListDungeons();

        if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != AllCategories)
            query = query.Where(d => string.Equals(d.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));

        var search = SearchText?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            string needle = search.ToLowerInvariant();
            query = query.Where(d => d.SearchBlob.Contains(needle, StringComparison.Ordinal));
        }

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
            EditableInsight = save.Insight.ToString();
            EditableEchoes = save.Echoes.ToString();
            EditableLevel = save.Level.ToString();
            HasLoadedSave = true;
            _config.Settings.LastSavePath = path;
            _config.Save();
            RefreshSlots();
            SnapshotBaselines();
            _undoSlot = null;
            _undoBytes = null;
            CanUndo = false;
            LoadSelectedSlot();
            RefreshSoundFixStatus(); // the opened save's folder may need the shadPS4 sound fix
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
            var rejected = ApplyCharacterEdits();

            // With auto-backup on, keep the managed timestamped backup as the single copy;
            // otherwise fall back to the rolling .bak next to the save. Never both.
            bool autoBackup = _config.Settings.AutoBackupEnabled;
            if (autoBackup && _saves.CurrentPath is { } path)
                _backups.Create(path, "before save");

            _saves.Save(createBackup: !autoBackup);
            SnapshotBaselines();
            StatusMessage = rejected.Count == 0
                ? $"Saved as {CharacterName}. A backup was written first."
                : $"Saved (backup written), but these were invalid and not applied: {string.Join(", ", rejected)}.";
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
        StatusMessage = $"Placed {SelectedDungeon.Glyph} in {SlotName(SelectedSlot.Number)}. Save to write it to disk.";
    }

    // Place the selected catalogue dungeon into all six slots at once (farming).
    private void FillAllSlots()
    {
        if (!HasLoadedSave) { StatusMessage = "Open a save first."; return; }
        if (SelectedDungeon is null) { StatusMessage = "Pick a dungeon from the list first."; return; }

        byte[] bytes = SelectedDungeon.Bytes;
        WriteSlots(Slots.Select(s => s.Number).ToList(), _ => bytes);
        StatusMessage = $"Filled all {Slots.Count} slots with {SelectedDungeon.Glyph}. Save to write to disk.";
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
        StatusMessage = $"Cleared {SlotName(SelectedSlot.Number)}. Save to write it to disk.";
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
        StatusMessage = $"Built dungeon placed in {SlotName(SelectedSlot.Number)}. Save to write it to disk.";
    }

    // Friendly slot name for status messages: slot 0 is the makeshift altar ("M").
    private static string SlotName(int number) => number == 0 ? "the makeshift altar" : $"slot {number}";

    // Write a run of altar slots and refresh them, then reload the selected slot once.
    // Used by fill-all, altar paste and apply-list so the loop lives in one place.
    private void WriteSlots(IReadOnlyList<int> targets, Func<int, byte[]> recordFor)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            _saves.SetSlot(targets[i], recordFor(i));
            SlotViewModel? slot = Slots.FirstOrDefault(s => s.Number == targets[i]);
            if (slot is not null)
                RefreshSlot(slot);
        }
        LoadSelectedSlot();
    }

    // Copy the selected slot's 125-byte record as a hex string for the clipboard.
    public string? CopySelectedSlotHex()
    {
        if (SelectedSlotBytes is not { } bytes)
        {
            StatusMessage = "Pick a slot with a dungeon first.";
            return null;
        }
        StatusMessage = "Dungeon copied to the clipboard as a 125-byte hex string.";
        return Convert.ToHexString(bytes);
    }

    // Paste a 125-byte hex string from the clipboard into the selected slot.
    public void PasteSlotHex(string? hex)
    {
        if (!HasLoadedSave || SelectedSlot is null)
        {
            StatusMessage = "Open a save and pick a slot first.";
            return;
        }

        string compact = DungeonShare.CompactHex(hex);
        if (compact.Length != DungeonStructure.Size * 2)
        {
            StatusMessage = $"Paste needs a {DungeonStructure.Size}-byte hex string " +
                            $"({DungeonStructure.Size * 2} hex chars); the clipboard had {compact.Length / 2}.";
            return;
        }

        int slot = SelectedSlot.Number;
        PlaceBuiltDungeon(Convert.FromHexString(compact));
        StatusMessage = $"Pasted dungeon into slot {slot}. Save to write it to disk.";
    }

    // Copy every altar slot (makeshift + the six stored, 125 bytes each) as one hex string.
    public string? CopyAltarHex()
    {
        if (!HasLoadedSave)
        {
            StatusMessage = "Open a save first.";
            return null;
        }
        var all = new byte[DungeonStructure.Size * Slots.Count];
        for (int i = 0; i < Slots.Count; i++)
            _saves.GetSlotBytes(Slots[i].Number).CopyTo(all, i * DungeonStructure.Size);
        StatusMessage = $"All {Slots.Count} altar slots copied to the clipboard.";
        return Convert.ToHexString(all);
    }

    // Replace altar slots from one hex string. Accepts the full set (makeshift + the
    // six stored) or a legacy 6-slot code, which fills slots 1-6 only.
    public void PasteAltarHex(string? hex)
    {
        if (!HasLoadedSave)
        {
            StatusMessage = "Open a save first.";
            return;
        }

        string compact = DungeonShare.CompactHex(hex);
        int bytes = compact.Length / 2;

        int[] targets;
        if (bytes == DungeonStructure.Size * Slots.Count)
            targets = Slots.Select(s => s.Number).ToArray();
        else if (bytes == DungeonStructure.Size * 6)
            targets = Enumerable.Range(1, 6).ToArray();
        else
        {
            StatusMessage = $"Altar paste needs {DungeonStructure.Size * Slots.Count} bytes " +
                            $"(or a legacy {DungeonStructure.Size * 6}); the clipboard had {bytes}.";
            return;
        }

        byte[] all = Convert.FromHexString(compact);
        WriteSlots(targets, i => all[(i * DungeonStructure.Size)..((i + 1) * DungeonStructure.Size)]);
        StatusMessage = $"Pasted {targets.Length} altar slots. Save to write them to disk.";
    }

    // --- Lists: create, rename, delete, add, remove, apply, share ---

    private const string MyDungeonsListName = "My dungeons";

    public async Task CreateListAsync(string name)
    {
        var list = await _lists.CreateListAsync(name);
        await LoadListsAsync();
        SelectedList = Lists.FirstOrDefault(l => l.Id == list.Id) ?? SelectedList;
        StatusMessage = $"Created list \"{name}\".";
    }

    public async Task RenameSelectedListAsync(string name)
    {
        if (SelectedList is not { Source: ListSource.User } list)
            return;
        await _lists.RenameListAsync(list.Id, name);
        await LoadListsAsync();
        StatusMessage = $"Renamed list to \"{name}\".";
    }

    public async Task DeleteSelectedListAsync()
    {
        if (SelectedList is not { Source: ListSource.User } list)
            return;
        string name = list.Name;
        await _lists.DeleteListAsync(list.Id);
        await LoadListsAsync();
        StatusMessage = $"Deleted list \"{name}\".";
    }

    // Create a new list and add the currently selected dungeon to it (from the flyout's
    // "New list..." entry, so a fresh install isn't a dead end).
    public async Task CreateListAndAddSelectedAsync(string name)
    {
        if (SelectedDungeon is null)
        {
            StatusMessage = "Pick a dungeon first.";
            return;
        }
        var list = await _lists.CreateListAsync(name);
        await _lists.AddDungeonAsync(list.Id, SelectedDungeon.Id);
        await LoadListsAsync();
        StatusMessage = $"Created \"{name}\" and added {SelectedDungeon.Glyph}.";
    }

    // Add the selected catalogue dungeon to one of the user's lists.
    public async Task AddSelectedDungeonToListAsync(int listId)
    {
        if (SelectedDungeon is null)
        {
            StatusMessage = "Pick a dungeon first.";
            return;
        }
        bool added = await _lists.AddDungeonAsync(listId, SelectedDungeon.Id);
        await LoadListsAsync();
        StatusMessage = added ? "Added to the list." : "That dungeon is already in the list.";
    }

    // Remove the selected dungeon from the current (user) list.
    public async Task RemoveSelectedFromListAsync()
    {
        if (SelectedList is not { Source: ListSource.User } list || SelectedDungeon is null)
        {
            StatusMessage = "Pick a dungeon in one of your own lists.";
            return;
        }
        await _lists.RemoveItemAsync(list.Id, SelectedDungeon.Id);
        await LoadListsAsync();
        StatusMessage = "Removed from the list.";
    }

    // Save the selected slot's dungeon into the My dungeons list.
    public async Task SaveCurrentSlotAsCustomAsync(string name)
    {
        if (SelectedSlotBytes is not { } bytes)
        {
            StatusMessage = "Pick a slot with a dungeon first.";
            return;
        }
        int listId = await EnsureMyDungeonsListId();
        await _lists.AddNewDungeonAsync(listId, name, null, bytes);
        await LoadDungeonsAsync();
        StatusMessage = $"Saved \"{name}\" to My dungeons.";
    }

    // The altar in the order lists map to it: the six real stored slots first, then the
    // makeshift altar (slot 0). So a shared six-dungeon list fills the six real slots
    // rather than wasting the first entry on the makeshift altar, and save<->apply
    // round-trips.
    private static readonly int[] AltarOrder = { 1, 2, 3, 4, 5, 6, 0 };

    // Save the altar (its non-empty slots) as a new user list.
    public async Task SaveAltarAsListAsync(string name)
    {
        if (!HasLoadedSave)
        {
            StatusMessage = "Open a save first.";
            return;
        }
        var list = await _lists.CreateListAsync(name);
        foreach (int number in AltarOrder)
        {
            var bytes = _saves.GetSlotBytes(number);
            if (DungeonStructure.IsEmpty(bytes))
                continue;
            await _lists.AddNewDungeonAsync(list.Id, $"Slot {number}", null, bytes);
        }
        await LoadDungeonsAsync();
        SelectedList = Lists.FirstOrDefault(l => l.Id == list.Id) ?? SelectedList;
        StatusMessage = $"Saved the altar as list \"{name}\".";
    }

    // The selected catalogue dungeon as a one-item share code (for right-click "copy code").
    public string? SelectedDungeonCode()
    {
        if (SelectedDungeon is not { } d)
            return null;
        StatusMessage = $"Copied {d.Glyph} as a share code.";
        return DungeonShare.Encode(new ShareSet(DungeonShare.CurrentVersion,
            new[] { new ShareItem(d.DisplayName, d.Category, d.Bytes) }));
    }

    // The selected list as a share code, or null (with a message) if it's empty.
    public string? BuildSelectedListCode()
    {
        if (SelectedList is null || SelectedList.Items.Count == 0)
        {
            StatusMessage = "Pick a non-empty list to share.";
            return null;
        }
        return ListSharing.Export(SelectedList);
    }

    // Export the selected list as a share code for the clipboard.
    public string? ShareSelectedList()
    {
        if (BuildSelectedListCode() is not { } code)
            return null;
        StatusMessage = $"Copied \"{SelectedList!.Name}\" as a share code.";
        return code;
    }

    // Let the App set a status message (e.g. after saving a list to a file).
    public void Notify(string message) => StatusMessage = message;

    // Import a shared list/dungeon from a code or file into a new user list.
    public async Task ImportSharedAsync(string? code)
    {
        ShareSet set;
        try { set = ListSharing.Import(code); }
        catch (FormatException) { StatusMessage = "That is not a dungeon share code."; return; }

        var items = set.Items
            .Select(i => (string.IsNullOrWhiteSpace(i.Name) ? "Imported" : i.Name, i.Category, i.Bytes))
            .ToList();
        var list = await _lists.ImportIntoNewListAsync("Imported list", items);
        await LoadDungeonsAsync();
        SelectedList = Lists.FirstOrDefault(l => l.Id == list.Id) ?? SelectedList;
        StatusMessage = $"Imported {items.Count} dungeon(s) into a new list.";
    }

    private async Task<int> EnsureMyDungeonsListId()
    {
        var mine = Lists.FirstOrDefault(l => l.Source == ListSource.User && l.Name == MyDungeonsListName);
        if (mine is not null)
            return mine.Id;
        var created = await _lists.CreateListAsync(MyDungeonsListName);
        return created.Id;
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

    // Applies the name typed in the box to the in-memory save; written on Save.
    // Applies the name, insight, echoes and level from the boxes to the in-memory save.
    // Returns the labels of any fields whose input was invalid, so Save can say so
    // instead of silently dropping them.
    private List<string> ApplyCharacterEdits()
    {
        var rejected = new List<string>();
        if (!HasLoadedSave)
            return rejected;

        string name = (EditableName ?? string.Empty).Trim();
        if (name != CharacterName)
        {
            if (name.Length is >= 1 and <= 16)
            {
                _saves.SetCharacterName(name);
                CharacterName = name;
            }
            else
            {
                rejected.Add("name (1-16 characters)");
            }
        }

        ApplyUInt(EditableInsight, "Insight", _saves.Insight, _saves.SetInsight, rejected);
        ApplyUInt(EditableEchoes, "Echoes", _saves.Echoes, _saves.SetEchoes, rejected);
        ApplyUInt(EditableLevel, "Level", _saves.Level, _saves.SetLevel, rejected);
        return rejected;
    }

    private static void ApplyUInt(string text, string label, uint current, Action<uint> set, List<string> rejected)
    {
        string trimmed = (text ?? "").Trim();
        if (uint.TryParse(trimmed, out uint value))
        {
            if (value != current)
                set(value);
        }
        else if (trimmed.Length > 0)
        {
            rejected.Add(label);
        }
    }

    // Applies the shadPS4 sound-crash workaround (Nexus mod 165) to every save
    // folder's options file (userdata0010), backing each up first.
    private void ApplySoundFix()
    {
        string? root = ResolveShadRoot();
        if (root is null)
        {
            StatusMessage = "Couldn't find a shadPS4 folder. Set it in Settings.";
            return;
        }

        int applied = 0, already = 0;
        foreach (var folder in _locator.FindSaveFolders(root))
        {
            var system = _locator.FindSystemFile(folder);
            if (system is null)
                continue;
            if (_locator.IsSoundFixApplied(system))
            {
                already++;
                continue;
            }
            _backups.Create(system, "before sound fix");
            _locator.ApplySoundFix(system);
            applied++;
        }

        StatusMessage = applied > 0
            ? $"Sound crash fix applied to {applied} folder(s). A backup was made first."
            : already > 0
                ? "Sound crash fix was already applied."
                : "No options file (userdata0010) found to fix.";

        RefreshSoundFixStatus();
    }

    // The configured shadPS4 folder, or the best guess if none is set.
    private string? ResolveShadRoot()
    {
        string? configured = _config.Settings.ShadPs4FolderPath;
        return !string.IsNullOrWhiteSpace(configured) && System.IO.Directory.Exists(configured)
            ? configured
            : _locator.GuessShadPs4Root();
    }

    // Starts shadPS4: the program set in Settings if any, otherwise the one found
    // next to the save folder (or the usual install locations).
    private void LaunchShadPs4()
    {
        string? program = null;

        string? configured = _config.Settings.ShadPs4ExePath;
        if (!string.IsNullOrWhiteSpace(configured) && (System.IO.File.Exists(configured) || System.IO.Directory.Exists(configured)))
        {
            program = configured;
        }
        else
        {
            string? root = ResolveShadRoot();
            if (root is null)
            {
                StatusMessage = "Couldn't find a shadPS4 folder. Set the program in Settings.";
                return;
            }
            program = _locator.FindProgram(root);
        }

        if (program is null)
        {
            StatusMessage = "Couldn't find the shadPS4 program. Pick it in Settings, under the shadPS4 folder.";
            return;
        }

        try
        {
            _locator.Launch(program);
            StatusMessage = "Launching shadPS4...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't launch shadPS4: {ex.Message}";
        }
    }

    // Flags the sidebar fix button when any save folder's options file is unpatched.
    private void RefreshSoundFixStatus()
    {
        bool needed = false;

        // The folder of the save that's actually open - covers saves opened by hand from
        // a non-standard location where auto-detect finds no shadPS4 root.
        if (_saves.CurrentPath is { } path && System.IO.Path.GetDirectoryName(path) is { } saveFolder)
        {
            string? system = _locator.FindSystemFile(saveFolder);
            if (system is not null && !_locator.IsSoundFixApplied(system))
                needed = true;
        }

        // Otherwise scan every detected save folder under the shadPS4 root.
        if (!needed && ResolveShadRoot() is { } root)
        {
            foreach (var folder in _locator.FindSaveFolders(root))
            {
                string? system = _locator.FindSystemFile(folder);
                if (system is not null && !_locator.IsSoundFixApplied(system))
                {
                    needed = true;
                    break;
                }
            }
        }

        SoundFixNeeded = needed;
    }

    private void DetectSaves()
    {
        DetectedSaves.Clear();

        var root = ResolveShadRoot();
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

        RefreshSoundFixStatus();
    }

    // --- Editing the selected slot's headstone fields ---

    private void ApplyRite(int index, Headstone.Rite rite)
    {
        if (_suppressEdits || !HasLoadedSave || SelectedSlot is null)
            return;

        CaptureUndo();
        _saves.SetRite(SelectedSlot.Number, index, rite);
        this.RaisePropertyChanged(nameof(RiteWarning));
        AfterEdit($"Rite {index + 1}: {rite}.");
    }

    private void ApplyPoison(bool enabled)
    {
        if (_suppressEdits || !HasLoadedSave || SelectedSlot is null || !PoisonPossible)
            return;

        CaptureUndo();
        _saves.SetPoison(SelectedSlot.Number, enabled);
        AfterEdit(enabled ? "Poison on." : "Poison off.");
    }

    private void ApplyFourthLayer(bool open)
    {
        if (_suppressEdits || !HasLoadedSave || SelectedSlot is null || !FourthLayerPossible)
            return;

        CaptureUndo();
        _saves.SetFourthLayer(SelectedSlot.Number, open);
        AfterEdit(open ? "4th layer opened." : "4th layer closed.");
    }

    private void ApplyDifficulty(bool up)
    {
        if (_suppressEdits || !HasLoadedSave || SelectedSlot is null || !DifficultyPossible)
            return;

        CaptureUndo();
        _saves.SetDifficulty(SelectedSlot.Number, up);
        AfterEdit(up ? "Difficulty up." : "Difficulty normal.");
    }

    private void ApplySpecialEnemy(Headstone.SpecialEnemy? option)
    {
        if (_suppressEdits || !HasLoadedSave || SelectedSlot is null || option is null)
            return;

        CaptureUndo();
        _saves.SetSpecialEnemy(SelectedSlot.Number, option.Value);
        AfterEdit("Special enemy set.");
    }

    private void ApplyField(Headstone.HeadstoneField field, string hex)
    {
        if (_suppressEdits || !HasLoadedSave || SelectedSlot is null)
            return;
        CaptureUndo();
        if (!_saves.TrySetField(SelectedSlot.Number, field, hex))
            return;

        SlotHexDump = _saves.SlotHexDump(SelectedSlot.Number);
        SelectedSlotBytes = _saves.GetSlotBytes(SelectedSlot.Number);
        RefreshSlot(SelectedSlot);
        StatusMessage = $"{field.Name} set. Save to write it to disk.";
    }

    private void AfterEdit(string message)
    {
        if (SelectedSlot is null)
            return;

        SlotHexDump = _saves.SlotHexDump(SelectedSlot.Number);
        SelectedSlotBytes = _saves.GetSlotBytes(SelectedSlot.Number);
        RefreshSlot(SelectedSlot);
        StatusMessage = $"{message} Save to write it to disk.";
    }

    // Remember each slot's bytes as they are on disk, so the live view can colour
    // only the bytes changed since (reset on load and after every save).
    private void SnapshotBaselines()
    {
        foreach (var s in Slots)
            _slotBaselines[s.Number] = HasLoadedSave ? _saves.GetSlotBytes(s.Number) : null;
        UpdateSelectedSlotBaseline();
    }

    private void UpdateSelectedSlotBaseline()
        => SelectedSlotBaseline = SelectedSlot is { } slot ? _slotBaselines[slot.Number] : null;

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
                PoisonPossible = FourthLayerPossible = DifficultyPossible = false;
                PoisonEnabled = FourthLayerOpen = DifficultyUp = false;
                SpecialEnemyOptions.Clear();
                foreach (var field in Fields) field.Set(string.Empty);
                SlotHexDump = string.Empty;
                SelectedSlotBytes = null;
                return;
            }

            var record = _saves.GetSlotBytes(SelectedSlot.Number);
            SlotHexDump = _saves.SlotHexDump(SelectedSlot.Number);
            SelectedSlotBytes = record;
            SelectedSlotOffset = _saves.SlotOffset(SelectedSlot.Number);
            UpdateSelectedSlotBaseline();
            for (int i = 0; i < Fields.Count; i++)
                Fields[i].Set(Headstone.ReadFieldHex(record, Fields[i].Field));

            if (DungeonStructure.IsEmpty(record))
            {
                SelectedSlotType = "empty";
                SelectedSlotJoin = "";
                foreach (var rite in RiteSlots) rite.Set(Headstone.Rite.None);
                PoisonPossible = FourthLayerPossible = DifficultyPossible = false;
                PoisonEnabled = FourthLayerOpen = DifficultyUp = false;
                SpecialEnemyOptions.Clear();
                return;
            }

            SelectedSlotType = Headstone.DungeonType(record);
            SelectedSlotJoin = Headstone.JoinRequirementsLabel(Headstone.JoinRequirementsHex(record));
            for (int i = 0; i < RiteSlots.Count; i++)
                RiteSlots[i].Set(Headstone.ReadRite(record, Headstone.RiteSlotOffsets[i]));
            this.RaisePropertyChanged(nameof(RiteWarning));

            PoisonPossible = Headstone.PoisonPossible(record);
            PoisonEnabled = Headstone.IsPoisoned(record);
            FourthLayerPossible = Headstone.FourthLayerPossible(record);
            FourthLayerOpen = Headstone.IsFourthLayerOpen(record);
            DifficultyPossible = Headstone.DifficultyPossible(record);
            DifficultyUp = Headstone.IsDifficultyUp(record);
            SpecialEnemyOptions.Clear();
            foreach (var option in Headstone.SpecialEnemyOptions(record))
                SpecialEnemyOptions.Add(option);
            SelectedSpecialEnemy = Headstone.ReadSpecialEnemy(record);
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
            slot.Occupied = !DungeonStructure.IsEmpty(record);
        }
        catch
        {
            slot.Occupied = false;
        }
    }

}
