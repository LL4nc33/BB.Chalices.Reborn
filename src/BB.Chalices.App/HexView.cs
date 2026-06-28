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

    // The last-saved bytes; only the bytes that differ from this get a field colour.
    public static readonly AttachedProperty<byte[]?> BaselineProperty =
        AvaloniaProperty.RegisterAttached<HexView, SelectableTextBlock, byte[]?>("Baseline");

    public static void SetBytes(SelectableTextBlock o, byte[]? v) => o.SetValue(BytesProperty, v);
    public static byte[]? GetBytes(SelectableTextBlock o) => o.GetValue(BytesProperty);
    public static void SetBaseline(SelectableTextBlock o, byte[]? v) => o.SetValue(BaselineProperty, v);
    public static byte[]? GetBaseline(SelectableTextBlock o) => o.GetValue(BaselineProperty);

    static HexView()
    {
        BytesProperty.Changed.Subscribe(e => { if (e.Sender is SelectableTextBlock t) Render(t); });
        BaselineProperty.Changed.Subscribe(e => { if (e.Sender is SelectableTextBlock t) Render(t); });
    }

    private static void Render(SelectableTextBlock target)
    {
        var inlines = target.Inlines;
        if (inlines is null)
            return;

        inlines.Clear();
        byte[]? bytes = GetBytes(target);
        byte[]? baseline = GetBaseline(target);
        if (bytes is null || bytes.Length == 0)
            return;

        for (int row = 0; row < bytes.Length; row += 16)
        {
            inlines.Add(new Run($"{row:X4}  ") { Foreground = ByteFieldPalette.Gutter });

            int count = Math.Min(16, bytes.Length - row);
            for (int i = 0; i < count; i++)
                inlines.Add(new Run($"{bytes[row + i]:X2} ") { Foreground = BrushFor(row + i, bytes, baseline) });
            for (int i = count; i < 16; i++)
                inlines.Add(new Run("   "));

            inlines.Add(new Run(" "));
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[row + i];
                char c = b is >= 32 and < 127 ? (char)b : '.';
                inlines.Add(new Run(c.ToString()) { Foreground = BrushFor(row + i, bytes, baseline) });
            }

            if (row + 16 < bytes.Length)
                inlines.Add(new Run("\n"));
        }
    }

    // A changed byte gets its field colour; everything unchanged stays neutral.
    private static Avalonia.Media.IBrush BrushFor(int offset, byte[] bytes, byte[]? baseline)
    {
        bool changed = baseline is not null && offset < baseline.Length && bytes[offset] != baseline[offset];
        return changed ? ByteFieldPalette.OffsetBrush(offset) : ByteFieldPalette.Neutral;
    }
}
