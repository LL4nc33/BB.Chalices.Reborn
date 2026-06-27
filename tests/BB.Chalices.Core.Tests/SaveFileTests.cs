using BB.Chalices.Core.Binary;
using BB.Chalices.Core.Saves;

namespace BB.Chalices.Core.Tests;

public class SaveFileTests
{
    // A synthetic save: the inventory marker at a 4-aligned offset, and enough
    // room for the headstone and flag regions that follow it.
    private static SaveFile MakeSave(out byte[] buffer)
    {
        buffer = new byte[105000];
        buffer[1000] = 0x40;
        buffer[1001] = 0xF0;
        buffer[1002] = 0xFF;
        buffer[1003] = 0xFF;
        return new SaveFile(buffer);
    }

    [Fact]
    public void GetSlotBytes_ReturnsTheRecordAtTheSlotOffset()
    {
        var save = MakeSave(out var buffer);
        int offset = save.GetSlotOffset(1);
        buffer[offset] = 0xAB;
        buffer[offset + 124] = 0xCD;

        var record = save.GetSlotBytes(1);

        Assert.Equal(125, record.Length);
        Assert.Equal(0xAB, record[0]);
        Assert.Equal(0xCD, record[124]);
    }

    [Fact]
    public void WriteSlotRaw_RoundTripsThroughGetSlotBytes()
    {
        var save = MakeSave(out _);

        var record = save.GetSlotBytes(2);
        Headstone.RiteBytes(Headstone.Rite.Cursed).CopyTo(record, Headstone.RiteSlot1Offset);
        save.WriteSlotRaw(2, record);

        var read = save.GetSlotBytes(2);
        Assert.Equal(Headstone.Rite.Cursed, Headstone.ReadRite(read, Headstone.RiteSlot1Offset));
    }

    [Fact]
    public void HexDumpSlot_ShowsOnlyTheSlotsBytesInFileContext()
    {
        var save = MakeSave(out _);

        var dump = save.HexDumpSlot(1);

        Assert.Contains("Offset(h)", dump);
        Assert.Contains("..", dump); // bytes belonging to other slots are masked
    }

    [Fact]
    public void SetSlot_WritesTheDungeonAndStampsTheDiscoveryFlag()
    {
        var save = MakeSave(out var buffer);

        var dungeon = new byte[DungeonStructure.Size];
        dungeon[0] = 0x12;
        save.SetSlot(3, dungeon);

        Assert.Equal(0x12, save.GetSlotBytes(3)[0]);

        int flagOffset = SaveFileReader.GetFlagsOffset(save.InventoryOffset, 3);
        Assert.Equal(0x30, buffer[flagOffset]); // first byte of the known flag pattern
    }
}
