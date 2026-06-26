using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BB.Chalices.ViewModels;

namespace BB.Chalices.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
}
