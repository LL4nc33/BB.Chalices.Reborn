using System.Collections.Generic;
using Avalonia.Media;

namespace BB.Chalices.App;

// One legend row: a field, the offsets it covers and its colour.
public sealed record ByteLegendEntry(string Name, string Range, IBrush Brush);

// DrAnger's colour map for the 125-byte headstone record. One brush per field,
// shared by the live-bytes view, the rite-slot borders and the legend window.
public static class ByteFieldPalette
{
    private static SolidColorBrush B(string hex) => new(Color.Parse(hex));

    public static readonly IBrush Gutter      = B("#6E6E78"); // offsets + ascii
    public static readonly IBrush Neutral     = B("#D2C9B0"); // unchanged bytes
    public static readonly IBrush Start       = B("#9A9AA4"); // the 1D start byte
    public static readonly IBrush AreaDepth   = B("#C0392B");
    public static readonly IBrush LayoutSeed  = B("#8E2A22");
    public static readonly IBrush Serial      = B("#8B5A2B");
    public static readonly IBrush Filler      = B("#4A4A52"); // zeros / padding
    public static readonly IBrush JoinReq     = B("#D98DA8");
    public static readonly IBrush Special     = B("#CA6F1E");
    public static readonly IBrush Unique      = B("#C9A227");
    public static readonly IBrush Gem         = B("#8FA82E");
    public static readonly IBrush FourthLayer = B("#2E8B57");
    public static readonly IBrush Poison      = B("#B39DDB");
    public static readonly IBrush RiteFetid   = B("#4DB6C9");
    public static readonly IBrush RiteRotted  = B("#2980B9");
    public static readonly IBrush RiteCursed  = B("#3949AB");
    public static readonly IBrush RiteUnused  = B("#7E94A8");
    public static readonly IBrush CreatorPsn  = B("#8C8C8C");
    public static readonly IBrush CharName    = B("#B0B0B0");

    // The colour for one byte by its offset in the 125-byte record.
    public static IBrush OffsetBrush(int o) => o switch
    {
        0x00 or 0x0C => Start,
        0x01 or 0x0D => AreaDepth,
        (>= 0x02 and <= 0x03) or (>= 0x0E and <= 0x0F) => LayoutSeed,
        >= 0x04 and <= 0x07 => Serial,
        >= 0x08 and <= 0x0B => Filler,
        >= 0x10 and <= 0x13 => JoinReq,
        >= 0x14 and <= 0x1B => Special,
        >= 0x1C and <= 0x23 => Unique,
        >= 0x24 and <= 0x2B => Gem,
        >= 0x2C and <= 0x33 => FourthLayer,
        >= 0x34 and <= 0x3B => Poison,
        >= 0x3C and <= 0x43 => RiteFetid,
        >= 0x44 and <= 0x4B => RiteRotted,
        >= 0x4C and <= 0x53 => RiteCursed,
        >= 0x54 and <= 0x5B => RiteUnused,
        >= 0x5C and <= 0x6B => CreatorPsn,
        >= 0x6C and <= 0x7B => CharName,
        _ => Filler,
    };

    // Rite slots 1-4 start at 0x3C and are 8 bytes apart.
    public static IBrush RiteBrush(int index) => OffsetBrush(0x3C + index * 8);

    // The named fields shown in the legend window (mirror and filler omitted).
    public static readonly IReadOnlyList<ByteLegendEntry> Legend =
    [
        new("Area and depth", "01", AreaDepth),
        new("Layout seed", "02-03", LayoutSeed),
        new("Dungeon ID on server", "04-07", Serial),
        new("Join requirements", "10-13", JoinReq),
        new("Special enemy / shop", "14-1B", Special),
        new("Unique item", "1C-23", Unique),
        new("Gem effect", "24-2B", Gem),
        new("4th layer", "2C-33", FourthLayer),
        new("Poison", "34-3B", Poison),
        new("Rite 1 - Fetid", "3C-43", RiteFetid),
        new("Rite 2 - Rotted", "44-4B", RiteRotted),
        new("Rite 3 - Cursed", "4C-53", RiteCursed),
        new("Rite 4 - unused", "54-5B", RiteUnused),
        new("Creator PSN", "5C-6B", CreatorPsn),
        new("Character name", "6C-7B", CharName),
    ];
}
