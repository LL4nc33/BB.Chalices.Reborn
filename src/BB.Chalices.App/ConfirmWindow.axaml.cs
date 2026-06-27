using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BB.Chalices.App;

public partial class ConfirmWindow : Window
{
    public ConfirmWindow()
    {
        InitializeComponent();
    }

    public ConfirmWindow(string message) : this()
    {
        MessageText.Text = message;
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirm(object? sender, RoutedEventArgs e) => Close(true);
}
