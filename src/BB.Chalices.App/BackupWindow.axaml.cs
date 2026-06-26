using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BB.Chalices.Services;

namespace BB.Chalices.App;

public partial class BackupWindow : Window
{
    private readonly BackupService _backups;
    private readonly string? _savePath;
    private readonly ObservableCollection<BackupInfo> _items = new();

    public bool RestorePerformed { get; private set; }

    public BackupWindow()
    {
        InitializeComponent();
        _backups = null!;
    }

    public BackupWindow(BackupService backups, string? savePath)
    {
        _backups = backups;
        _savePath = savePath;
        InitializeComponent();

        BackupList.ItemsSource = _items;
        Refresh();
    }

    private void Refresh()
    {
        _items.Clear();
        foreach (var backup in _backups.GetAll())
            _items.Add(backup);
    }

    private void OnCreate(object? sender, RoutedEventArgs e)
    {
        if (_savePath is not null)
            _backups.Create(_savePath, "manual");
        Refresh();
    }

    private void OnRestore(object? sender, RoutedEventArgs e)
    {
        if (_savePath is null || BackupList.SelectedItem is not BackupInfo backup)
            return;

        _backups.Restore(_savePath, backup);
        RestorePerformed = true;
        Refresh();
    }

    private void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (BackupList.SelectedItem is BackupInfo backup)
        {
            _backups.Delete(backup);
            Refresh();
        }
    }

    private async void OnOpenFolder(object? sender, RoutedEventArgs e)
    {
        if (Directory.Exists(_backups.BackupDirectory))
            await Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(_backups.BackupDirectory));
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close(RestorePerformed);
}
