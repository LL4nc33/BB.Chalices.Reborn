using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using BB.Chalices.Services;
using BB.Chalices.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BB.Chalices.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnWindowOpened;
        Closing += OnWindowClosing;
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        // Clicking in a column points the text-size +/- buttons at it. Tunnel handlers
        // fire from the outside in, so the editor (innermost) wins over the middle panel.
        SidebarPanel.AddHandler(PointerPressedEvent, OnSidebarPressed, RoutingStrategies.Tunnel);
        MiddlePanel.AddHandler(PointerPressedEvent, (_, _) => SelectZoom(ZoomTarget.Catalogue), RoutingStrategies.Tunnel);
        EditorPanel.AddHandler(PointerPressedEvent, (_, _) => SelectZoom(ZoomTarget.Editor), RoutingStrategies.Tunnel);
    }

    private void OnSidebarPressed(object? sender, PointerPressedEventArgs e)
    {
        // The zoom controls live in the sidebar; clicking them must not re-target it.
        if (IsWithin(e.Source, ZoomControls))
            return;
        SelectZoom(ZoomTarget.Sidebar);
    }

    private static bool IsWithin(object? source, Visual container)
    {
        for (Visual? v = source as Visual; v is not null; v = v.GetVisualParent())
            if (ReferenceEquals(v, container))
                return true;
        return false;
    }

    private void SelectZoom(ZoomTarget target)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.SelectZoomTarget(target);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
        => e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        var file = e.DataTransfer.TryGetFiles()?.OfType<IStorageFile>().FirstOrDefault();
        if (file?.TryGetLocalPath() is not { } path)
            return;

        // A .bbc file imports as a list; anything else opens as a save.
        if (path.EndsWith(".bbc", StringComparison.OrdinalIgnoreCase))
            await viewModel.ImportSharedAsync(await File.ReadAllTextAsync(path));
        else
            await viewModel.LoadSaveCommand.Execute(path);
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        if (Services?.GetService<ConfigService>() is not { Settings: { } settings })
            return;

        if (settings.WindowWidth is > 0 && settings.WindowHeight is > 0)
        {
            Width = settings.WindowWidth.Value;
            Height = settings.WindowHeight.Value;
        }
        if (settings.WindowMaximized)
            WindowState = WindowState.Maximized;
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (Services?.GetService<ConfigService>() is not { Settings: { } settings })
            return;

        settings.WindowMaximized = WindowState == WindowState.Maximized;
        if (WindowState == WindowState.Normal && !double.IsNaN(Width) && !double.IsNaN(Height))
        {
            settings.WindowWidth = (int)Width;
            settings.WindowHeight = (int)Height;
        }
        Services.GetRequiredService<ConfigService>().Save();
    }

    private void OnCatalogueDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.ApplyDungeonCommand.Execute().Subscribe();
    }

    private void OnBackupDoubleTapped(object? sender, TappedEventArgs e) => ConfirmRestore();
    private void OnRestoreBackup(object? sender, RoutedEventArgs e) => ConfirmRestore();

    private async void ConfirmRestore()
    {
        if (DataContext is not MainViewModel { SelectedBackup: { } backup } viewModel)
            return;

        bool confirmed = await new ConfirmWindow($"Restore this backup?\n\n{backup.DisplayName}\n\nYour current save is backed up first.")
            .ShowDialog<bool>(this);
        if (confirmed)
            viewModel.RestoreBackupCommand.Execute().Subscribe();
    }

    private async void OnOpenSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a Bloodborne save (userdata…)",
            AllowMultiple = false,
        });

        if (files.Count > 0)
            await viewModel.LoadSaveCommand.Execute(files[0].Path.LocalPath);
    }

    private void OnShowCatalogue(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.CurrentView = AppView.Catalogue;
    }

    private void OnShowSettings(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.OpenSettings();
    }

    private void OnShowGuide(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.CurrentView = AppView.Guide;
    }

    // Table-of-contents jump: the button's Tag names the chapter anchor to scroll to.
    private void OnGuideNavigate(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.Tag is string anchor)
            this.FindControl<Control>(anchor)?.BringIntoView();
    }

    private async void OnNewList(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;
        string? name = await new PromptWindow("Name your new list:", "My list").ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(name))
            await viewModel.CreateListAsync(name);
    }

    private async void OnRenameList(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel { SelectedList: { } list } viewModel)
            return;
        string? name = await new PromptWindow("Rename this list:", list.Name).ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(name))
            await viewModel.RenameSelectedListAsync(name);
    }

    private async void OnDeleteList(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel { SelectedList: { } list } viewModel)
            return;
        bool confirmed = await new ConfirmWindow($"Delete the list \"{list.Name}\"?\n\nThe dungeons themselves are not deleted.")
            .ShowDialog<bool>(this);
        if (confirmed)
            await viewModel.DeleteSelectedListAsync();
    }

    private async void OnShareList(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.ShareSelectedList() is { } code && Clipboard is { } clipboard)
            await clipboard.SetTextAsync(code);
    }

    private async void OnImportList(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && Clipboard is { } clipboard)
            await viewModel.ImportSharedAsync(await clipboard.TryGetTextAsync());
    }

    private async void OnSaveAltarAsList(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;
        string? name = await new PromptWindow("Save the altar as a list. Name it:", "My altar").ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(name))
            await viewModel.SaveAltarAsListAsync(name);
    }

    private async void OnExportListFile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel || viewModel.BuildSelectedListCode() is not { } code)
            return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the list as a dungeon file",
            SuggestedFileName = "list.bbc",
            DefaultExtension = "bbc",
        });
        if (file is null)
            return;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(code);
        viewModel.Notify($"Saved the list to {file.Name}.");
    }

    private async void OnNewListAndAdd(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;
        string? name = await new PromptWindow("Name your new list:", "My list").ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(name))
            await viewModel.CreateListAndAddSelectedAsync(name);
    }

    // --- Right-click context menus (built in code to keep the dynamic lists reliable) ---

    private static MenuItem MenuAction(string header, Action action, bool enabled = true)
    {
        var item = new MenuItem { Header = header, IsEnabled = enabled };
        item.Click += (_, _) => action();
        return item;
    }

    private void OnCatalogueContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || e.Source is not Control src
            || src.DataContext is not DungeonViewModel dungeon)
            return;
        vm.SelectedDungeon = dungeon;

        var menu = new ContextMenu();

        // Place directly into a chosen altar slot; selecting the slot also jumps the
        // editor over to it so you see the result.
        var placeIn = new MenuItem { Header = "Place in slot", IsEnabled = vm.CanPlaceDungeon };
        foreach (var s in vm.Slots)
        {
            SlotViewModel slotRef = s;
            placeIn.Items.Add(MenuAction(s.Number == 0 ? "Makeshift (M)" : $"Slot {s.Number}", () =>
            {
                vm.SelectedSlot = slotRef;
                vm.ApplyDungeonCommand.Execute().Subscribe();
            }));
        }
        menu.Items.Add(placeIn);
        menu.Items.Add(MenuAction("Fill all slots", () => vm.FillAllSlotsCommand.Execute().Subscribe(), vm.CanPlaceDungeon));

        var addTo = new MenuItem { Header = "Add to list" };
        addTo.Items.Add(MenuAction("+ New list...", () => OnNewListAndAdd(this, new RoutedEventArgs())));
        foreach (var list in vm.UserLists.ToList())
        {
            int id = list.Id;
            addTo.Items.Add(MenuAction(list.Name, async () => await vm.AddSelectedDungeonToListAsync(id)));
        }
        menu.Items.Add(addTo);

        if (vm.CanEditSelectedList)
            menu.Items.Add(MenuAction("Remove from this list", async () => await vm.RemoveSelectedFromListAsync()));

        menu.Items.Add(new Separator());
        menu.Items.Add(MenuAction("Copy as code", async () =>
        {
            if (vm.SelectedDungeonCode() is { } code && Clipboard is { } cb)
                await cb.SetTextAsync(code);
        }));

        menu.Open(src);
        e.Handled = true;
    }

    private void OnSlotContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || e.Source is not Control src
            || src.DataContext is not SlotViewModel slot)
            return;
        vm.SelectedSlot = slot;

        var menu = new ContextMenu();
        menu.Items.Add(MenuAction("Copy", async () =>
        {
            if (vm.CopySelectedSlotHex() is { } hex && Clipboard is { } cb)
                await cb.SetTextAsync(hex);
        }));
        menu.Items.Add(MenuAction("Paste", async () =>
        {
            if (Clipboard is { } cb)
                vm.PasteSlotHex(await cb.TryGetTextAsync());
        }));
        menu.Items.Add(MenuAction("Save to My dungeons", async () =>
        {
            string? name = await new PromptWindow("Save this dungeon to your catalogue. Name it:", "My dungeon")
                .ShowDialog<string?>(this);
            if (!string.IsNullOrWhiteSpace(name))
                await vm.SaveCurrentSlotAsCustomAsync(name);
        }));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuAction("Clear slot", () => vm.ClearSlotCommand.Execute().Subscribe()));

        menu.Open(src);
        e.Handled = true;
    }

    private void OnBackupContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || e.Source is not Control src
            || src.DataContext is not BackupInfo backup)
            return;
        vm.SelectedBackup = backup;

        var menu = new ContextMenu();
        menu.Items.Add(MenuAction("Restore this backup", ConfirmRestore));
        menu.Items.Add(MenuAction("Delete", () => OnDeleteBackup(src, new RoutedEventArgs())));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuAction("Open folder", () => OnOpenBackupFolder(src, new RoutedEventArgs())));

        menu.Open(src);
        e.Handled = true;
    }

    private async void OnAddToListPicked(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (sender is Avalonia.Controls.ListBox { SelectedItem: BB.Chalices.Data.Entities.DungeonList list } box
            && DataContext is MainViewModel viewModel)
        {
            await viewModel.AddSelectedDungeonToListAsync(list.Id);
            box.SelectedItem = null;
        }
    }

    private async void OnRemoveFromList(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            await viewModel.RemoveSelectedFromListAsync();
    }

    private void OnApplyList(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.ApplyListToAltar();
    }

    private void OnSaveSettings(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.SaveSettings();
    }

    private async void OnBrowseShad(object? sender, RoutedEventArgs e)
    {
        if (await PickFolderAsync() is { } path && DataContext is MainViewModel viewModel)
            viewModel.ShadPs4Path = path;
    }

    private async void OnBrowseBackup(object? sender, RoutedEventArgs e)
    {
        if (await PickFolderAsync() is { } path && DataContext is MainViewModel viewModel)
        {
            viewModel.BackupPath = path;
            viewModel.PersistBackupPath();
        }
    }

    private void OnAutoBackupChanged(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.PersistAutoBackup();
    }

    private async Task<string?> PickFolderAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    private async void OnOpenBackupFolder(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && !string.IsNullOrWhiteSpace(viewModel.BackupPath)
            && Directory.Exists(viewModel.BackupPath))
            await Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(viewModel.BackupPath));
    }

    private async void OnOpenDataFolder(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && !string.IsNullOrWhiteSpace(viewModel.DataFolder)
            && Directory.Exists(viewModel.DataFolder))
            await Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(viewModel.DataFolder));
    }

    private async void OnOpenLink(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string url })
            await Launcher.LaunchUriAsync(new System.Uri(url));
    }

    private void OnBuilderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.OpenBuilder();
    }

    private void OnCloseBuilder(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.CloseBuilder();
    }

    private void OnBuilderRandom(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.Builder.RandomLayout();
    }

    private void OnBuilderPlaceSlot(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && sender is MenuItem { Tag: string tag } && int.TryParse(tag, out int slot))
            viewModel.PlaceBuiltInSlot(slot);
    }

    private void OnBuilderAddToList(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel || sender is not Control src)
            return;

        var menu = new ContextMenu();
        menu.Items.Add(MenuAction("+ New list...", async () =>
        {
            string? name = await new PromptWindow("Name your new list:", "My list").ShowDialog<string?>(this);
            if (!string.IsNullOrWhiteSpace(name))
                await viewModel.CreateListAndAddBuiltAsync(name);
        }));
        foreach (var list in viewModel.UserLists.ToList())
        {
            int id = list.Id;
            menu.Items.Add(MenuAction(list.Name, async () => await viewModel.AddBuiltToListAsync(id)));
        }
        menu.Open(src);
    }

    private async void OnBuilderSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;
        string? name = await new PromptWindow("Save this dungeon to your catalogue. Name it:", "My dungeon")
            .ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(name))
            await viewModel.SaveBuiltAsCustomAsync(name);
    }

    private void OnShowBackups(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.OpenBackups();
    }

    private async void OnLegendClick(object? sender, RoutedEventArgs e)
        => await new LegendWindow().ShowDialog(this);

    private async void OnCopyAltar(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.CopyAltarHex() is { } hex && Clipboard is { } clipboard)
            await clipboard.SetTextAsync(hex);
    }

    private async void OnPasteAltar(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && Clipboard is { } clipboard)
            viewModel.PasteAltarHex(await clipboard.TryGetTextAsync());
    }

    private async void OnDeleteBackup(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel { SelectedBackup: { } backup } viewModel)
            return;

        bool confirmed = await new ConfirmWindow($"Delete this backup?\n\n{backup.DisplayName}\n\nThis cannot be undone.")
            .ShowDialog<bool>(this);
        if (confirmed)
            viewModel.DeleteSelectedBackup();
    }

    private static IServiceProvider? Services => (Application.Current as App)?.Services;
}
