using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

    private async void OnBackupDoubleTapped(object? sender, TappedEventArgs e)
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

    private void OnShowNox(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.ShowNox = true;
    }

    private void OnShowAll(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.ShowNox = false;
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
            viewModel.BackupPath = path;
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

    private async void OnBuilderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel main || Services is not { } services)
            return;

        var viewModel = new DungeonBuilderViewModel(services.GetRequiredService<DungeonService>());
        await viewModel.InitAsync();
        await new DungeonBuilderWindow(main) { DataContext = viewModel }.ShowDialog(this);
    }

    private void OnShowBackups(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.OpenBackups();
    }

    private async void OnLegendClick(object? sender, RoutedEventArgs e)
        => await new LegendWindow().ShowDialog(this);

    private async void OnCopySlotHex(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.CopySelectedSlotHex() is { } hex && Clipboard is { } clipboard)
            await clipboard.SetTextAsync(hex);
    }

    private async void OnPasteSlotHex(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && Clipboard is { } clipboard)
            viewModel.PasteSlotHex(await clipboard.TryGetTextAsync());
    }

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

    private async void OnSaveCustom(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        string? name = await new PromptWindow("Save this dungeon to your catalogue. Name it:", "My dungeon")
            .ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(name))
            await viewModel.SaveCurrentSlotAsCustomAsync(name);
    }

    private async void OnDeleteCustom(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel { SelectedDungeon: { IsCustom: true } dungeon } viewModel)
            return;

        bool confirmed = await new ConfirmWindow($"Remove \"{dungeon.Description ?? dungeon.Glyph}\" from your dungeons?")
            .ShowDialog<bool>(this);
        if (confirmed)
            await viewModel.DeleteSelectedCustomAsync();
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
