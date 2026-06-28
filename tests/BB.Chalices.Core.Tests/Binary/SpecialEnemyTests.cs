using BB.Chalices.Core.Binary;

namespace BB.Chalices.Core.Tests.Binary;

public class SpecialEnemyTests
{
    [Theory]
    [InlineData(0x35, Headstone.SpecialEnemy.Bath, 0x50)]    // Isz
    [InlineData(0x35, Headstone.SpecialEnemy.Patches, 0x52)] // Isz
    [InlineData(0x0A, Headstone.SpecialEnemy.Bath, 0x1E)]    // Pthumeru
    [InlineData(0x0A, Headstone.SpecialEnemy.Default, 0xFF)] // Pthumeru
    public void SpecialEnemyByte_DiffersByArea(byte area, Headstone.SpecialEnemy option, int expected)
    {
        bool isz = area == 0x35;
        Assert.Equal((byte)expected, Headstone.SpecialEnemyByte(option, isz));
    }

    [Theory]
    [InlineData(0x35, Headstone.SpecialEnemy.AllBps)]    // Isz
    [InlineData(0x0A, Headstone.SpecialEnemy.PatchesBps)] // Pthumeru
    public void SmartSpecialEnemy_RoundTrips(byte area, Headstone.SpecialEnemy option)
    {
        var record = new byte[125];
        record[1] = area;
        Headstone.SmartSpecialEnemy(record, option).CopyTo(record, Headstone.SpecialEnemyOffset);

        Assert.Equal(option, Headstone.ReadSpecialEnemy(record));
    }

    [Fact]
    public void SpecialEnemyOptions_SinisterExcludesBps()
    {
        var sinister = new byte[125];
        sinister[1] = 0x35;
        sinister[2] = 0x14; // sinister layout seed

        var options = Headstone.SpecialEnemyOptions(sinister);

        Assert.DoesNotContain(Headstone.SpecialEnemy.AllBps, options);
        Assert.DoesNotContain(Headstone.SpecialEnemy.BathBps, options);
        Assert.Contains(Headstone.SpecialEnemy.Patches, options);
    }

    [Fact]
    public void Difficulty_OnlyPthumeru5_AndRoundTrips()
    {
        var pthu5 = new byte[125];
        pthu5[1] = 0x32;
        Assert.True(Headstone.DifficultyPossible(pthu5));

        var isz = new byte[125];
        isz[1] = 0x35;
        Assert.False(Headstone.DifficultyPossible(isz));

        Headstone.DifficultyBytes(true).CopyTo(pthu5, Headstone.GemEffectOffset);
        Assert.True(Headstone.IsDifficultyUp(pthu5));

        Headstone.DifficultyBytes(false).CopyTo(pthu5, Headstone.GemEffectOffset);
        Assert.False(Headstone.IsDifficultyUp(pthu5));
    }
}
