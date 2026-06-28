using BB.Chalices.Core.Binary;

namespace BB.Chalices.Core.Tests.Binary;

public class DungeonStructureTests
{
    [Fact]
    public void Empty_MatchesTheGameWrittenEmptySlot()
    {
        // The exact 125-byte record the game writes for an unoccupied altar slot:
        // FFFFFFFF map, zeros, 80 marker, an FF block over every effect/rite field,
        // a zeroed creator/name block and a 02 terminator.
        byte[] data = DungeonStructure.Empty().Data.ToArray();

        Assert.Equal(DungeonStructure.Size, data.Length);
        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, data[0..4]);
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, data[4..8]);
        Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00 }, data[8..12]);
        Assert.All(data[0x0C..0x5C], b => Assert.Equal(0xFF, b)); // 0x0C-0x5B
        Assert.All(data[0x5C..0x7C], b => Assert.Equal(0x00, b)); // 0x5C-0x7B
        Assert.Equal(0x02, data[0x7C]);

        Assert.True(new DungeonStructure(data).IsEmpty());
    }
}
