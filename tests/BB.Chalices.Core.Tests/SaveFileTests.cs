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

    [Fact]
    public void SetSlot_Slot6AtExactBoundary_StampsFlagWithoutThrowing()
    {
        var buffer = new byte[1000 + 103454]; // the smallest valid save for the marker at 1000
        buffer[1000] = 0x40;
        buffer[1001] = 0xF0;
        buffer[1002] = 0xFF;
        buffer[1003] = 0xFF;
        var save = new SaveFile(buffer);

        save.SetSlot(6, new byte[DungeonStructure.Size]);

        int flagOffset = SaveFileReader.GetFlagsOffset(save.InventoryOffset, 6);
        Assert.Equal(0x30, buffer[flagOffset]);
    }

    [Fact]
    public void Construct_OneByteTooSmall_ThrowsInvalidOperationException()
    {
        var buffer = new byte[1000 + 103453]; // one byte under the boundary
        buffer[1000] = 0x40;
        buffer[1001] = 0xF0;
        buffer[1002] = 0xFF;
        buffer[1003] = 0xFF;

        Assert.Throws<InvalidOperationException>(() => new SaveFile(buffer));
    }

    [Fact]
    public void SetSlot_RoundTripsDungeonAndStampsFullFlagPattern()
    {
        var save = MakeSave(out var buffer);

        int flagOffset = SaveFileReader.GetFlagsOffset(save.InventoryOffset, 3);
        // Pre-fill the flag region so we know the pattern is written, not left over.
        for (int i = 0; i < DungeonStructure.Size; i++)
            buffer[flagOffset + i] = 0xFF;

        var dungeon = new byte[DungeonStructure.Size];
        for (int i = 0; i < dungeon.Length; i++)
            dungeon[i] = (byte)(i + 1); // distinct bytes 0x01..0x7D

        save.SetSlot(3, dungeon);

        Assert.Equal(dungeon, save.GetSlotBytes(3)); // full record round-trips

        var flagHead = buffer.AsSpan(flagOffset, 7).ToArray();
        Assert.Equal(new byte[] { 0x30, 0x00, 0x03, 0xE8, 0x00, 0x04, 0x00 }, flagHead);
        Assert.Equal(0x03, buffer[flagOffset + 16]); // the second part of the discovery pattern
        Assert.Equal(0x02, buffer[flagOffset + 17]);
    }

    [Fact]
    public void SetSlot_FullRecordRoundTripsThroughGetSlotBytes()
    {
        var save = MakeSave(out _);

        var record = new byte[DungeonStructure.Size];
        for (int i = 0; i < record.Length; i++)
            record[i] = (byte)(255 - i);

        save.SetSlot(4, record);

        Assert.Equal(record, save.GetSlotBytes(4));
    }

    [Fact]
    public void WriteSlotRaw_EmptyDungeon_RoundTripsAsEmpty()
    {
        var save = MakeSave(out _);

        save.WriteSlotRaw(5, DungeonStructure.Empty().Data);

        Assert.True(save.GetSlot(5).IsEmpty());
    }

    [Fact]
    public void SetSlot_AndWriteSlotRaw_WrongLength_ThrowArgumentException()
    {
        var save = MakeSave(out _);

        Assert.Throws<ArgumentException>(() => save.SetSlot(1, new byte[10]));
        Assert.Throws<ArgumentException>(() => save.WriteSlotRaw(1, new byte[10]));
    }

    [Fact]
    public void SetSlot_WritingOneSlot_LeavesNeighboursUntouched()
    {
        var save = MakeSave(out _);

        var slot1 = new byte[DungeonStructure.Size];
        var slot3 = new byte[DungeonStructure.Size];
        for (int i = 0; i < DungeonStructure.Size; i++)
        {
            slot1[i] = 0x11;
            slot3[i] = 0x33;
        }
        save.WriteSlotRaw(1, slot1);
        save.WriteSlotRaw(3, slot3);

        var before1 = save.GetSlotBytes(1);
        var before3 = save.GetSlotBytes(3);

        var slot2 = new byte[DungeonStructure.Size];
        for (int i = 0; i < DungeonStructure.Size; i++)
            slot2[i] = 0x22;
        save.SetSlot(2, slot2);

        Assert.Equal(slot2, save.GetSlotBytes(2));
        Assert.Equal(before1, save.GetSlotBytes(1));
        Assert.Equal(before3, save.GetSlotBytes(3));
    }
}
