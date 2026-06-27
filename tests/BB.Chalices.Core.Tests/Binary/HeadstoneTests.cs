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

    [Fact]
    public void Field_ReadAndParseRoundTrips()
    {
        var field = Headstone.Fields.Single(f => f.Name == "Join requirements"); // 0x10, 4 bytes
        var record = new byte[125];
        Convert.FromHexString("0000196D").CopyTo(record, field.Offset);

        Assert.Equal("0000196D", Headstone.ReadFieldHex(record, field));

        Assert.True(Headstone.TryParseField("00 00 18 4B", field, out var bytes)); // whitespace ignored
        Assert.Equal(new byte[] { 0x00, 0x00, 0x18, 0x4B }, bytes);

        Assert.False(Headstone.TryParseField("00184B", field, out _));     // wrong length
        Assert.False(Headstone.TryParseField("ZZZZZZZZ", field, out _));   // not hex
    }

    [Theory]
    [InlineData("00001909", true, 0x0D)]   // Pthumeru 4: 0D on
    [InlineData("00001909", false, 0x0E)]  // Pthumeru 4: 0E off
    [InlineData("0000184B", true, 0x0A)]   // Hintertomb 2: 0A on
    [InlineData("0000184B", false, 0xFF)]  // Hintertomb 2: FF off
    [InlineData("DEADBEEF", true, 0xFF)]   // unknown: FF either way
    [InlineData("DEADBEEF", false, 0xFF)]
    public void SmartPoison_LastByteFollowsJoinRequirements(string joinHex, bool enabled, int expectedLast)
    {
        var record = new byte[125];
        Convert.FromHexString(joinHex).CopyTo(record, Headstone.JoinRequirementsOffset);

        var poison = Headstone.SmartPoison(record, enabled);

        Assert.Equal((byte)expectedLast, poison[Headstone.FieldLength - 1]);
    }

    [Theory]
    [InlineData(0x0A, true)]
    [InlineData(0x0D, true)]
    [InlineData(0xFF, false)]
    [InlineData(0x0E, false)]
    public void IsPoisoned_ReadsTheLastPoisonByte(int lastByte, bool expected)
    {
        var record = new byte[125];
        record[Headstone.PoisonOffset + Headstone.FieldLength - 1] = (byte)lastByte;

        Assert.Equal(expected, Headstone.IsPoisoned(record));
    }

    [Fact]
    public void PoisonBytes_BuildsTemplateWithControlByte()
    {
        Assert.Equal(Convert.FromHexString("FFFFFFFFFFFFFF0D"), Headstone.PoisonBytes(0x0D));
    }

    [Theory]
    [InlineData(0x3C, true)]
    [InlineData(0x3D, true)]
    [InlineData(0x3F, true)]
    [InlineData(0x41, true)]
    [InlineData(0x43, true)]
    [InlineData(0xFF, false)]
    public void IsFourthLayerOpen_ChecksTheFunctionalByte(int functionalByte, bool expected)
    {
        var record = new byte[125];
        record[Headstone.FourthLayerOffset + Headstone.FieldLength - 1] = (byte)functionalByte;

        Assert.Equal(expected, Headstone.IsFourthLayerOpen(record));
    }

    [Theory]
    [InlineData(0x3C)] // Hintertomb / Pthumeru 2 / Loran open byte
    [InlineData(0x3D)] // Pthumeru 4 / 5 open byte
    [InlineData(0x43)] // Isz 5 open byte
    public void IsFourthLayerOpen_TrueForBytesEmittedByControl(byte openByte)
    {
        var record = new byte[125];
        Headstone.FourthLayerBytes(openByte).CopyTo(record, Headstone.FourthLayerOffset);

        Assert.True(Headstone.IsFourthLayerOpen(record));
    }

    [Theory]
    [InlineData("00001909", true, true)]   // Pthumeru 4: both possible
    [InlineData("DEADBEEF", false, false)] // unknown: neither possible
    [InlineData("0000198B", false, true)]  // Isz 5: poison excluded, 4th layer possible
    public void PoisonPossibleAndFourthLayerPossible_FollowJoinRequirements(
        string joinHex, bool poisonPossible, bool fourthLayerPossible)
    {
        var record = new byte[125];
        Convert.FromHexString(joinHex).CopyTo(record, Headstone.JoinRequirementsOffset);

        Assert.Equal(poisonPossible, Headstone.PoisonPossible(record));
        Assert.Equal(fourthLayerPossible, Headstone.FourthLayerPossible(record));
    }

    [Fact]
    public void IsPoisonedAndIsFourthLayerOpen_ShortRecord_ReturnFalse()
    {
        Assert.False(Headstone.IsPoisoned(new byte[10]));
        Assert.False(Headstone.IsFourthLayerOpen(new byte[10]));
    }
}
