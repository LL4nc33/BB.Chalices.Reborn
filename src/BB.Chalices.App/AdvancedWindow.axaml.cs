using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BB.Chalices.App;

public partial class AdvancedWindow : Window
{
    public AdvancedWindow()
    {
        InitializeComponent();
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
