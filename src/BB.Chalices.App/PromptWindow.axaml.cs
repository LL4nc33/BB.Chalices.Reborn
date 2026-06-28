using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace BB.Chalices.App;

// A small text-input dialog. ShowDialog<string?> returns the trimmed text, or null
// if cancelled or left blank.
public partial class PromptWindow : Window
{
    public PromptWindow()
    {
        InitializeComponent();
    }

    public PromptWindow(string message, string? initial = null) : this()
    {
        MessageText.Text = message;
        InputText.Text = initial ?? "";
        Opened += (_, _) => { InputText.Focus(); InputText.SelectAll(); };
        InputText.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
                Confirm();
            else if (e.Key == Key.Escape)
                Close(null);
        };
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);

    private void OnConfirm(object? sender, RoutedEventArgs e) => Confirm();

    private void Confirm()
        => Close(string.IsNullOrWhiteSpace(InputText.Text) ? null : InputText.Text.Trim());
}
