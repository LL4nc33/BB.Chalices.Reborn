using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace BB.Chalices.App;

// Renders a byte[] as a colour-coded hex dump in a SelectableTextBlock: one colour
// per headstone field (ByteFieldPalette), with offsets and ascii in the gutter grey.
public class HexView
{
    private HexView() { }

    public static readonly AttachedProperty<byte[]?> BytesProperty =
        AvaloniaProperty.RegisterAttached<HexView, SelectableTextBlock, byte[]?>("Bytes");

    public static void SetBytes(SelectableTextBlock o, byte[]? v) => o.SetValue(BytesProperty, v);
    public static byte[]? GetBytes(SelectableTextBlock o) => o.GetValue(BytesProperty);

    static HexView()
    {
        BytesProperty.Changed.Subscribe(e =>
        {
            if (e.Sender is SelectableTextBlock target)
                Render(target, e.NewValue.GetValueOrDefault());
        });
    }

    private static void Render(SelectableTextBlock target, byte[]? bytes)
    {
        var inlines = target.Inlines;
        if (inlines is null)
            return;

        inlines.Clear();
        if (bytes is null || bytes.Length == 0)
            return;

        for (int row = 0; row < bytes.Length; row += 16)
        {
            inlines.Add(new Run($"{row:X4}  ") { Foreground = ByteFieldPalette.Gutter });

            int count = Math.Min(16, bytes.Length - row);
            for (int i = 0; i < count; i++)
                inlines.Add(new Run($"{bytes[row + i]:X2} ") { Foreground = ByteFieldPalette.OffsetBrush(row + i) });
            for (int i = count; i < 16; i++)
                inlines.Add(new Run("   "));

            inlines.Add(new Run(" "));
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[row + i];
                char c = b is >= 32 and < 127 ? (char)b : '.';
                inlines.Add(new Run(c.ToString()) { Foreground = ByteFieldPalette.OffsetBrush(row + i) });
            }

            if (row + 16 < bytes.Length)
                inlines.Add(new Run("\n"));
        }
    }
}
