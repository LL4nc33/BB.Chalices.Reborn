using BB.Chalices.Core.Binary;

namespace BB.Chalices.Core.Tests.Binary;

public class DungeonBuilderTests
{
    // A real, normal Isz 5 dungeon from the Hex Research Central "Examples" tab
    // (seed map 01, dungeon 0x4E). Used as a known-good base.
    private static byte[] IszBase() => Convert.FromHexString(
        ("1D 35 01 4E 00 99 B8 5E 00 00 00 00 1D 35 01 4E 00 00 19 82 " +
         "00 97 DF 06 00 00 00 53 00 97 DF 0B 00 00 00 27 00 97 DF 1A 00 00 00 35 " +
         "00 97 DF 24 00 00 00 44 00 97 DE F7 00 00 00 0F 00 97 DF 38 00 00 00 FF " +
         "00 97 DF 38 00 00 00 FF 00 97 DF 38 00 00 00 FF 00 97 DF 38 00 00 00 FF")
        .Replace(" ", "").PadRight(DungeonStructure.Size * 2, '0'));

    [Theory]
    [InlineData(DungeonBuilder.Variant.Normal, 0, 0x00)]
    [InlineData(DungeonBuilder.Variant.Normal, 1, 0x01)]
    [InlineData(DungeonBuilder.Variant.Defiled, 0, 0x0A)]
    [InlineData(DungeonBuilder.Variant.Defiled, 1, 0x0B)]
    [InlineData(DungeonBuilder.Variant.Sinister, 0, 0x14)]
    [InlineData(DungeonBuilder.Variant.Sinister, 1, 0x15)]
    public void LayoutMapByte_MatchesTheReference(DungeonBuilder.Variant variant, int mapIndex, int expected)
    {
        Assert.Equal((byte)expected, DungeonBuilder.LayoutMapByte(variant, mapIndex));
    }

    [Fact]
    public void Build_OverwritesStructuralBytesAndMirrorsThem()
    {
        var built = DungeonBuilder.Build(IszBase(), 0x35, DungeonBuilder.Variant.Sinister, 0, 0x0A);

        Assert.Equal(0x35, built[0x01]); // area+depth
        Assert.Equal(0x14, built[0x02]); // sinister, first map
        Assert.Equal(0x0A, built[0x03]); // dungeon number 10

        // the 0x0C-0x0F block mirrors 0x00-0x03
        Assert.Equal(built[0x01], built[0x0D]);
        Assert.Equal(built[0x02], built[0x0E]);
        Assert.Equal(built[0x03], built[0x0F]);
    }

    [Fact]
    public void Build_KeepsTheBaseSerialAndEffectStrings()
    {
        var baseRecord = IszBase();
        var built = DungeonBuilder.Build(baseRecord, 0x35, DungeonBuilder.Variant.Normal, 1, 0x4E);

        // serial (0x04-0x07) and join requirements (0x10-0x13) are untouched
        Assert.Equal(baseRecord[0x04..0x08], built[0x04..0x08]);
        Assert.Equal(baseRecord[0x10..0x14], built[0x10..0x14]);
        // the functional effect bytes survive (unique item, gem, 4th layer, poison)
        Assert.Equal(0x27, built[0x23]);
        Assert.Equal(0x35, built[0x2B]);
        Assert.Equal(0x44, built[0x33]);
        Assert.Equal(0x0F, built[0x3B]);
    }

    [Fact]
    public void Build_RejectsAWrongSizedBase()
    {
        Assert.Throws<ArgumentException>(() => DungeonBuilder.Build(new byte[10], 0x35, DungeonBuilder.Variant.Normal, 0, 0));
    }
}
