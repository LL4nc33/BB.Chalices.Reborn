using BB.Chalices.Core.Binary;
using Xunit;

namespace BB.Chalices.Core.Tests.Binary;

// The record-level writers are shared by the slot editor and the dungeon builder,
// so pin the round-trip: what ApplyRite stamps, ReadRite reads back.
public class HeadstoneApplyTests
{
    [Theory]
    [InlineData(0, Headstone.Rite.Fetid)]
    [InlineData(1, Headstone.Rite.Rotted)]
    [InlineData(2, Headstone.Rite.Cursed)]
    [InlineData(3, Headstone.Rite.Sinister)]
    [InlineData(0, Headstone.Rite.None)]
    public void ApplyRite_RoundTripsThroughReadRite(int index, Headstone.Rite rite)
    {
        var record = new byte[DungeonStructure.Size];

        Headstone.ApplyRite(record, index, rite);

        Assert.Equal(rite, Headstone.ReadRite(record, Headstone.RiteSlotOffsets[index]));
    }

    [Fact]
    public void ApplyRite_OnOneSlot_LeavesTheOtherSlotsUntouched()
    {
        var record = new byte[DungeonStructure.Size];
        Headstone.ApplyRite(record, 0, Headstone.Rite.Cursed);
        Headstone.ApplyRite(record, 2, Headstone.Rite.Fetid);

        Assert.Equal(Headstone.Rite.Cursed, Headstone.ReadRite(record, Headstone.RiteSlotOffsets[0]));
        Assert.Equal(Headstone.Rite.None, Headstone.ReadRite(record, Headstone.RiteSlotOffsets[1]));
        Assert.Equal(Headstone.Rite.Fetid, Headstone.ReadRite(record, Headstone.RiteSlotOffsets[2]));
    }

    [Fact]
    public void ApplyFourthLayer_WhenNotPossible_IsANoOp()
    {
        var record = new byte[DungeonStructure.Size];
        var before = record.ToArray();

        // A zeroed record does not support a 4th layer, so nothing should change.
        Headstone.ApplyFourthLayer(record, open: true);

        Assert.Equal(before, record);
    }

    [Fact]
    public void ApplyDifficulty_WhenNotPossible_IsANoOp()
    {
        var record = new byte[DungeonStructure.Size];
        var before = record.ToArray();

        Headstone.ApplyDifficulty(record, up: true);

        Assert.Equal(before, record);
    }
}
