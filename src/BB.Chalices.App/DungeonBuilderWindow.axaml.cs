using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BB.Chalices.ViewModels;

namespace BB.Chalices.App;

public partial class DungeonBuilderWindow : Window
{
    private readonly MainViewModel? _main;

    public DungeonBuilderWindow()
    {
        InitializeComponent();
    }

    public DungeonBuilderWindow(MainViewModel main) : this()
    {
        _main = main;
    }

    private void OnNext(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DungeonBuilderViewModel viewModel)
            viewModel.Next();
    }

    private void OnBack(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DungeonBuilderViewModel viewModel)
            viewModel.Back();
    }

    private void OnPlace(object? sender, RoutedEventArgs e)
    {
        if (_main is not null && DataContext is DungeonBuilderViewModel viewModel)
        {
            _main.PlaceBuiltDungeon(viewModel.Build());
            Close();
        }
    }

    private async void OnLearnMore(object? sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://docs.google.com/spreadsheets/d/1zFIzhnXHhYomlR-tFJcyk3cPywf1snqkDH900j6rtAI/edit?pli=1&gid=935899711#gid=935899711"));
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
