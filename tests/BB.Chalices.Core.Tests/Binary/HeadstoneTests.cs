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
    [InlineData("Loran5", true, 0x0A)]
    [InlineData("Loran5", false, 0xFF)]
    [InlineData("Isz5", true, 0x0F)]
    [InlineData("Other", true, 0xFF)]
    public void ExpectedPoisonByte_MatchesTable(string dungeonType, bool poisonOn, int expected)
    {
        Assert.Equal((byte)expected, Headstone.ExpectedPoisonByte(dungeonType, poisonOn));
    }

    [Theory]
    // Loran 5 (map 0x34) can be forced, but only Hintertomb 2/3 and Pthumeru 4/5 are
    // poison under normal generation.
    [InlineData(0x34, true, false)]  // Loran 5: toggleable, not normal
    [InlineData(0x28, true, true)]   // Pthumeru 4: toggleable and normal
    [InlineData(0x0A, false, false)] // Pthumeru 1: not poison (unchanged)
    [InlineData(0x35, false, false)] // Isz 5: never poison
    public void PoisonPossibleAndNormal_MatchDungeonType(byte mapByte, bool possible, bool normal)
    {
        var record = new byte[125];
        record[0] = 0x1D;
        record[1] = mapByte;

        Assert.Equal(possible, Headstone.PoisonPossible(record));
        Assert.Equal(normal, Headstone.PoisonNormallyAvailable(record));
    }

    [Fact]
    public void FourthLayerControl_PerArea()
    {
        Assert.Equal((true, (byte)0x3D, (byte)0x3E), Headstone.FourthLayerControl(AreaRecord(0x28)));        // Pthumeru 4
        Assert.Equal((true, (byte)0x3C, (byte)0xFF), Headstone.FourthLayerControl(AreaRecord(0x14)));        // Pthumeru 2
        Assert.Equal((true, (byte)0x43, (byte)0x44), Headstone.FourthLayerControl(AreaRecord(0x35)));        // Isz 5
        Assert.Equal((false, (byte)0x00, (byte)0x00), Headstone.FourthLayerControl(AreaRecord(0x0A)));       // Pthumeru 1
        Assert.Equal((false, (byte)0x00, (byte)0x00), Headstone.FourthLayerControl(AreaRecord(0x35, true))); // Sinister Isz 5
    }

    private static byte[] AreaRecord(byte areaByte, bool sinister = false)
    {
        var record = new byte[125];
        record[1] = areaByte;
        record[2] = sinister ? (byte)0x14 : (byte)0x00;
        return record;
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
    [InlineData(0x28, true, 0x0D)]   // Pthumeru 4: 0D on
    [InlineData(0x28, false, 0x0E)]  // Pthumeru 4: 0E off
    [InlineData(0x15, true, 0x0A)]   // Hintertomb 2: 0A on
    [InlineData(0x15, false, 0xFF)]  // Hintertomb 2: FF off
    [InlineData(0x0A, true, 0xFF)]   // Pthumeru 1: FF either way (no poison)
    [InlineData(0x0A, false, 0xFF)]
    public void SmartPoison_LastByteFollowsArea(int areaByte, bool enabled, int expectedLast)
    {
        var record = new byte[125];
        record[1] = (byte)areaByte;

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
    [InlineData(0x28, false, true, true)]    // Pthumeru 4: poison + 4th
    [InlineData(0x0A, false, false, false)]  // Pthumeru 1: neither
    [InlineData(0x35, false, false, true)]   // Isz 5: 4th only, poison excluded
    [InlineData(0x2A, false, true, true)]    // Loran 4: poison now forceable (0x0A) + 4th
    [InlineData(0x35, true, false, false)]   // Sinister Isz 5: 4th excluded
    public void PoisonAndFourthLayerPossible_FollowAreaAndSinister(
        int areaByte, bool sinister, bool poisonPossible, bool fourthLayerPossible)
    {
        var record = new byte[125];
        record[1] = (byte)areaByte;
        if (sinister)
            record[2] = 0x14;

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
