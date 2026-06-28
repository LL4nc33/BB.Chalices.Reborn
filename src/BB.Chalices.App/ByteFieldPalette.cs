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

    // Brightened for readability on the dark panels: every colour sits at a
    // luminance close to the parchment text, while staying distinct per field.
    public static readonly IBrush Gutter      = B("#9292A0"); // offsets + ascii
    public static readonly IBrush Neutral     = B("#E0D8C2"); // unchanged bytes
    public static readonly IBrush Start       = B("#B7B7C2"); // the 1D start byte
    public static readonly IBrush AreaDepth   = B("#E6584A");
    public static readonly IBrush LayoutSeed  = B("#EC9A84");
    public static readonly IBrush Serial      = B("#D9A85F");
    public static readonly IBrush Filler      = B("#71717C"); // zeros / padding
    public static readonly IBrush JoinReq     = B("#EBA1BB");
    public static readonly IBrush Special     = B("#EE9038");
    public static readonly IBrush Unique      = B("#E6C047");
    public static readonly IBrush Gem         = B("#BBD653");
    public static readonly IBrush FourthLayer = B("#4FCE88");
    public static readonly IBrush Poison      = B("#CBB8EC");
    public static readonly IBrush RiteFetid   = B("#6BD3E6");
    public static readonly IBrush RiteRotted  = B("#5FB1EA");
    public static readonly IBrush RiteCursed  = B("#8C99EE");
    public static readonly IBrush RiteUnused  = B("#A6BBCF");
    public static readonly IBrush CreatorPsn  = B("#B0B0B0");
    public static readonly IBrush CharName    = B("#CACACA");

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
