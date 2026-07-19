using BB.Chalices.Core.Binary;

namespace BB.Chalices.Core.Tests.Binary;

public class GemPoolTests
{
    [Fact]
    public void ActiveCategories_StandardIszDungeon_MatchesResearch()
    {
        // A normal Isz 5 dungeon activates GemCategories 11+12+22+34+45.
        var record = new byte[125];
        record[Headstone.GemEffectOffset + 7] = 0x35;    // gem    -> 11
        record[Headstone.UniqueItemOffset + 7] = 0x27;   // unique -> 45
        record[Headstone.SpecialEnemyOffset + 7] = 0x4F; // shop   -> 34
        record[Headstone.FourthLayerOffset + 7] = 0x43;  // 4th    -> 12
        record[Headstone.PoisonOffset + 7] = 0x0F;       // poison -> 22

        Assert.Equal(new[] { 11, 12, 22, 34, 45 }, GemPool.ActiveCategories(record));
        Assert.Contains("Arcane / Physical ATK %", GemPool.Favoured(record));
    }

    [Fact]
    public void ActiveCategories_LoranDungeon_FavoursFireAndBolt()
    {
        var record = new byte[125];
        record[Headstone.GemEffectOffset + 7] = 0x34; // gem -> 34 (Fire / Bolt)

        Assert.Equal(new[] { 34 }, GemPool.ActiveCategories(record));
        Assert.Equal("Fire / Bolt ATK %", GemPool.Favoured(record));
    }

    [Fact]
    public void Favoured_EmptyRecord_IsBlank()
    {
        Assert.Equal("", GemPool.Favoured(new byte[125]));
    }

    [Theory]
    [InlineData(0x32, "Radial")]   // Pthumeru 5
    [InlineData(0x1F, "Radial")]   // Hintertomb 3
    [InlineData(0x34, "Waning")]   // Loran 5
    [InlineData(0x35, "Triangle")] // Isz 5
    [InlineData(0x00, "")]         // unrecognised
    public void Shape_MapsTheAreaByteToTheChaliceGemShape(byte area, string expected)
    {
        var record = new byte[125];
        record[1] = area;
        Assert.Equal(expected, GemPool.Shape(record));
    }

    [Fact]
    public void Describe_CombinesShapeAndFavouredEffects()
    {
        var record = new byte[125];
        record[1] = 0x35;                              // Isz 5 -> Triangle
        record[Headstone.GemEffectOffset + 7] = 0x35;  // gem-effect -> Physical ATK %

        string desc = GemPool.Describe(record);

        Assert.StartsWith("Triangle gems - ", desc);
        Assert.Contains("Physical ATK %", desc);
    }
}
