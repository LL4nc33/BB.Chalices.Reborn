using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BB.Chalices.Services;

namespace BB.Chalices.App;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _config;

    public SettingsWindow()
    {
        InitializeComponent();
        _config = null!;
    }

    public SettingsWindow(ConfigService config)
    {
        _config = config;
        InitializeComponent();

        ShadBox.Text = config.Settings.ShadPs4FolderPath ?? string.Empty;
        BackupBox.Text = config.BackupDirectory;
        AutoBackupBox.IsChecked = config.Settings.AutoBackupEnabled;
    }

    private async void OnBrowseShad(object? sender, RoutedEventArgs e) => await PickFolderInto(ShadBox);
    private async void OnBrowseBackup(object? sender, RoutedEventArgs e) => await PickFolderInto(BackupBox);

    private async Task PickFolderInto(TextBox target)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        if (folders.Count > 0)
            target.Text = folders[0].Path.LocalPath;
    }

    private async void OnOpenBackup(object? sender, RoutedEventArgs e)
    {
        var path = BackupBox.Text;
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            await Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(path));
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        _config.Settings.ShadPs4FolderPath = Blank(ShadBox.Text);
        _config.Settings.BackupDirectory = Blank(BackupBox.Text);
        _config.Settings.AutoBackupEnabled = AutoBackupBox.IsChecked ?? true;
        _config.Save();
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();

    private async void OnOpenLink(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string url })
            await Launcher.LaunchUriAsync(new System.Uri(url));
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
