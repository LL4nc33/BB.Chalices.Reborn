using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BB.Chalices.App;

public partial class HexReferenceWindow : Window
{
    public HexReferenceWindow()
    {
        InitializeComponent();
        DataContext = HexReference.Sections;
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
