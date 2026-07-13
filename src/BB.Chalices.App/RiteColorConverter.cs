using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using BB.Chalices.Core.Binary;

namespace BB.Chalices.App;

// Maps a rite-slot index (0-3) to its legend colour, for the slot's coloured border.
public sealed class RiteColorConverter : IValueConverter
{
    public static readonly RiteColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int index ? ByteFieldPalette.RiteBrush(index) : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

// True when a width (e.g. a panel's Bounds.Width) is at least the threshold passed
// as the converter parameter. Used to hide the wide live-bytes dump when the editor
// panel is dragged too narrow to fit it without overlapping.
public sealed class WidthAtLeastConverter : IValueConverter
{
    public static readonly WidthAtLeastConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double width = value is double d ? d : 0;
        double threshold = parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var t)
            ? t : 0;
        return width >= threshold;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

// Maps a byte offset to its legend colour, for the string-field labels.
public sealed class OffsetColorConverter : IValueConverter
{
    public static readonly OffsetColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int offset ? ByteFieldPalette.OffsetBrush(offset) : ByteFieldPalette.Neutral;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

// Friendly labels for the special-enemy / shop dropdown.
public sealed class SpecialEnemyLabelConverter : IValueConverter
{
    public static readonly SpecialEnemyLabelConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Headstone.SpecialEnemy option
            ? option switch
            {
                Headstone.SpecialEnemy.Default => "Default (none)",
                Headstone.SpecialEnemy.Bath => "Bath messenger",
                Headstone.SpecialEnemy.AllBps => "All Beast-Possessed Souls",
                Headstone.SpecialEnemy.Patches => "Patches the Spider",
                Headstone.SpecialEnemy.BathBps => "Bath + BPS",
                Headstone.SpecialEnemy.PatchesBps => "Patches + BPS",
                _ => option.ToString(),
            }
            : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
