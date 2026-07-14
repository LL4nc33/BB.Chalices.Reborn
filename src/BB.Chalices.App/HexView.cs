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

    // The record's absolute offset in the save file, so the dump shows real offsets
    // (and the leading/trailing ".." padding) just like the original hex view.
    public static readonly AttachedProperty<int> RecordStartProperty =
        AvaloniaProperty.RegisterAttached<HexView, SelectableTextBlock, int>("RecordStart");

    // When false, the trailing ascii column is dropped so the dump fits a narrow panel.
    public static readonly AttachedProperty<bool> ShowAsciiProperty =
        AvaloniaProperty.RegisterAttached<HexView, SelectableTextBlock, bool>("ShowAscii", defaultValue: true);

    // When false, the leading offset column is dropped too, leaving just the hex bytes.
    public static readonly AttachedProperty<bool> ShowOffsetProperty =
        AvaloniaProperty.RegisterAttached<HexView, SelectableTextBlock, bool>("ShowOffset", defaultValue: true);

    public static void SetBytes(SelectableTextBlock o, byte[]? v) => o.SetValue(BytesProperty, v);
    public static byte[]? GetBytes(SelectableTextBlock o) => o.GetValue(BytesProperty);
    public static void SetBaseline(SelectableTextBlock o, byte[]? v) => o.SetValue(BaselineProperty, v);
    public static byte[]? GetBaseline(SelectableTextBlock o) => o.GetValue(BaselineProperty);
    public static void SetRecordStart(SelectableTextBlock o, int v) => o.SetValue(RecordStartProperty, v);
    public static int GetRecordStart(SelectableTextBlock o) => o.GetValue(RecordStartProperty);
    public static void SetShowAscii(SelectableTextBlock o, bool v) => o.SetValue(ShowAsciiProperty, v);
    public static bool GetShowAscii(SelectableTextBlock o) => o.GetValue(ShowAsciiProperty);
    public static void SetShowOffset(SelectableTextBlock o, bool v) => o.SetValue(ShowOffsetProperty, v);
    public static bool GetShowOffset(SelectableTextBlock o) => o.GetValue(ShowOffsetProperty);

    static HexView()
    {
        BytesProperty.Changed.Subscribe(e => { if (e.Sender is SelectableTextBlock t) Render(t); });
        BaselineProperty.Changed.Subscribe(e => { if (e.Sender is SelectableTextBlock t) Render(t); });
        RecordStartProperty.Changed.Subscribe(e => { if (e.Sender is SelectableTextBlock t) Render(t); });
        ShowAsciiProperty.Changed.Subscribe(e => { if (e.Sender is SelectableTextBlock t) Render(t); });
        ShowOffsetProperty.Changed.Subscribe(e => { if (e.Sender is SelectableTextBlock t) Render(t); });
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

        bool showAscii = GetShowAscii(target);
        bool showOffset = GetShowOffset(target);
        int recordStart = GetRecordStart(target);
        int dumpStart = recordStart & ~0xF;
        int dumpEnd = (recordStart + bytes.Length + 0xF) & ~0xF;

        var gutter = ByteFieldPalette.Gutter;
        string header = (showOffset ? "Offset(h)  " : "") + "00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F";
        inlines.Add(new Run(header + "\n") { Foreground = gutter });
        inlines.Add(new Run(new string('-', header.Length) + "\n") { Foreground = gutter });

        for (int line = dumpStart; line < dumpEnd; line += 16)
        {
            if (showOffset)
                inlines.Add(new Run($"{line:X8}   ") { Foreground = gutter });

            for (int i = 0; i < 16; i++)
            {
                int rel = line + i - recordStart;
                if (rel >= 0 && rel < bytes.Length)
                    inlines.Add(new Run($"{bytes[rel]:X2} ") { Foreground = BrushFor(rel, bytes, baseline) });
                else
                    inlines.Add(new Run(".. ") { Foreground = gutter });
            }

            if (showAscii)
            {
                inlines.Add(new Run(" ") { Foreground = gutter });
                for (int i = 0; i < 16; i++)
                {
                    int rel = line + i - recordStart;
                    if (rel < 0 || rel >= bytes.Length)
                    {
                        inlines.Add(new Run(".") { Foreground = gutter });
                        continue;
                    }
                    byte b = bytes[rel];
                    char c = b is >= 32 and <= 126 ? (char)b : '.';
                    inlines.Add(new Run(c.ToString()) { Foreground = BrushFor(rel, bytes, baseline) });
                }
            }

            if (line + 16 < dumpEnd)
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
