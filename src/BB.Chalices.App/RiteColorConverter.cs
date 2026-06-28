using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

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
