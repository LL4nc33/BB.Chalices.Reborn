using BB.Chalices.Core.Binary;

namespace BB.Chalices.Core.Tests.Binary;

public class SaveFileReaderTests
{
    [Fact]
    public void FindInventoryMarker_ValidMarker_ReturnsCorrectOffset()
    {
        var buffer = new byte[2000];
        buffer[1000] = 0x40;
        buffer[1001] = 0xF0;
        buffer[1002] = 0xFF;
        buffer[1003] = 0xFF;

        int offset = SaveFileReader.FindInventoryMarker(buffer);

        Assert.Equal(1000, offset);
    }

    [Fact]
    public void FindInventoryMarker_NoMarker_ReturnsNegativeOne()
    {
        var buffer = new byte[1000];

        int offset = SaveFileReader.FindInventoryMarker(buffer);

        Assert.Equal(-1, offset);
    }

    [Theory]
    [InlineData(1, 88328)]   // Slot 1
    [InlineData(2, 88453)]   // Slot 2 (88328 + 125)
    [InlineData(3, 88578)]   // Slot 3 (88328 + 250)
    [InlineData(6, 88953)]   // Slot 6 (88328 + 625)
    public void GetHeadstoneOffset_ValidSlot_ReturnsCorrectOffset(int slot, int expectedOffset)
    {
        int offset = SaveFileReader.GetHeadstoneOffset(0, slot);

        Assert.Equal(expectedOffset, offset);
    }

    [Fact]
    public void GetHeadstoneOffset_WithInventoryOffset_CalculatesCorrectly()
    {
        int offset = SaveFileReader.GetHeadstoneOffset(1000, 1);

        Assert.Equal(89328, offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public void GetHeadstoneOffset_InvalidSlot_ThrowsException(int invalidSlot)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SaveFileReader.GetHeadstoneOffset(0, invalidSlot));
    }

    [Theory]
    [InlineData(1, 102704)]
    [InlineData(6, 103329)]  // 102704 + 625
    public void GetFlagsOffset_ValidSlot_ReturnsCorrectOffset(int slot, int expectedOffset)
    {
        int offset = SaveFileReader.GetFlagsOffset(0, slot);

        Assert.Equal(expectedOffset, offset);
    }

    [Fact]
    public void GetCharacterName_ValidData_ReturnsName()
    {
        var buffer = new byte[2000];
        int inventory = 1000;
        int nameOffset = inventory - 469;
        var nameBytes = System.Text.Encoding.ASCII.GetBytes("TestHunter");
        Array.Copy(nameBytes, 0, buffer, nameOffset, nameBytes.Length);

        string name = SaveFileReader.GetCharacterName(buffer, inventory);

        Assert.Equal("TestHunter", name);
    }

    [Fact]
    public void GetCharacterName_NullTerminated_ReadsCorrectly()
    {
        var buffer = new byte[2000];
        int inventory = 1000;
        int nameOffset = inventory - 469;
        var nameBytes = System.Text.Encoding.ASCII.GetBytes("Hunter\0ExtraGarbage");
        Array.Copy(nameBytes, 0, buffer, nameOffset, nameBytes.Length);

        string name = SaveFileReader.GetCharacterName(buffer, inventory);

        Assert.Equal("Hunter", name);
    }

    [Fact]
    public void ValidateSaveFileSize_ValidSize_ReturnsTrue()
    {
        Assert.True(SaveFileReader.ValidateSaveFileSize(200000, 50000));
    }

    [Fact]
    public void ValidateSaveFileSize_TooSmall_ReturnsFalse()
    {
        Assert.False(SaveFileReader.ValidateSaveFileSize(50000, 0));
    }
}
