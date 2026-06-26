using BB.Chalices.Core.Binary;

namespace BB.Chalices.Core.Tests.Binary;

public class HeadstoneTests
{
    [Theory]
    [InlineData(Headstone.Rite.Fetid, "00009CAE0000000B")]
    [InlineData(Headstone.Rite.Rotted, "00009CB80000001B")]
    [InlineData(Headstone.Rite.Cursed, "00009CEA00000049")]
    [InlineData(Headstone.Rite.Sinister, "0000000000000028")]
    [InlineData(Headstone.Rite.None, "FFFFFFFFFFFFFFFF")]
    public void RiteBytes_MatchKnownPresets(Headstone.Rite rite, string hex)
    {
        Assert.Equal(Convert.FromHexString(hex), Headstone.RiteBytes(rite));
    }

    [Fact]
    public void ReadRite_RoundTrips()
    {
        var record = new byte[125];
        Headstone.RiteBytes(Headstone.Rite.Cursed).CopyTo(record, Headstone.RiteSlot2Offset);

        Assert.Equal(Headstone.Rite.Cursed, Headstone.ReadRite(record, Headstone.RiteSlot2Offset));
        Assert.Equal(Headstone.Rite.None, Headstone.ReadRite(record, Headstone.RiteSlot1Offset));
    }

    [Theory]
    [InlineData(0x14, "Pthumeru 2")]
    [InlineData(0x1F, "Hintertomb 3")]
    [InlineData(0x35, "Isz 5")]
    [InlineData(0x00, "Unknown")]
    public void DungeonType_FromMapByte(byte mapByte, string expected)
    {
        var record = new byte[125];
        record[1] = mapByte;
        Assert.Equal(expected, Headstone.DungeonType(record));
    }

    [Theory]
    [InlineData("Pthumeru4", true, 0x0D)]
    [InlineData("Pthumeru4", false, 0x0E)]
    [InlineData("Hintertomb2", true, 0x0A)]
    [InlineData("Isz5", true, 0x0F)]
    [InlineData("Other", true, 0xFF)]
    public void ExpectedPoisonByte_MatchesTable(string dungeonType, bool poisonOn, int expected)
    {
        Assert.Equal((byte)expected, Headstone.ExpectedPoisonByte(dungeonType, poisonOn));
    }

    [Fact]
    public void FourthLayerControl_PerDungeonType()
    {
        Assert.Equal((true, (byte)0x3D, (byte)0x3E), Headstone.FourthLayerControl("00001909")); // Pthumeru 4
        Assert.Equal((true, (byte)0x43, (byte)0x44), Headstone.FourthLayerControl("0000198B")); // Isz 5
        Assert.Equal((false, (byte)0x00, (byte)0x00), Headstone.FourthLayerControl("DEADBEEF"));
    }

    [Fact]
    public void FourthLayerBytes_PrefixPlusControl()
    {
        Assert.Equal(Convert.FromHexString("0097DF240000003C"), Headstone.FourthLayerBytes(0x3C));
    }
}
