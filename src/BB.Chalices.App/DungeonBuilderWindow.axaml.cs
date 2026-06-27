using Avalonia.Controls;
using Avalonia.Interactivity;
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

    private void OnPlace(object? sender, RoutedEventArgs e)
    {
        if (_main is not null && DataContext is DungeonBuilderViewModel viewModel)
            _main.PlaceBuiltDungeon(viewModel.Build());
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
