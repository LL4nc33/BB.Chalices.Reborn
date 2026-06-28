using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BB.Chalices.App;

public partial class LegendWindow : Window
{
    public LegendWindow()
    {
        InitializeComponent();
        LegendList.ItemsSource = ByteFieldPalette.Legend;
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
