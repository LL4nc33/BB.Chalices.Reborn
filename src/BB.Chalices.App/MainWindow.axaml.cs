using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

    private async void OnBackupsClick(object? sender, RoutedEventArgs e)
    {
        if (Services is not { } services)
            return;

        var viewModel = DataContext as MainViewModel;
        var window = new BackupWindow(services.GetRequiredService<BackupService>(), viewModel?.CurrentSavePath);
        var restored = await window.ShowDialog<bool>(this);

        // A restore overwrites the file on disk, so reload it into the editor.
        if (restored && viewModel is not null && viewModel.CurrentSavePath is { } path)
            await viewModel.LoadSaveCommand.Execute(path);
    }

    private static IServiceProvider? Services => (Application.Current as App)?.Services;
}
