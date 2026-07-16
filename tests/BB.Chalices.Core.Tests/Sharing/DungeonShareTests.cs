using BB.Chalices.Core.Binary;
using BB.Chalices.Core.Sharing;
using Xunit;

namespace BB.Chalices.Core.Tests.Sharing;

public class DungeonShareTests
{
    private static byte[] SampleRecord(byte tag)
    {
        var data = DungeonStructure.Empty().Data.ToArray();
        data[4] = tag; // stamp the dungeon-id area so records differ
        return data;
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("00ff", "00ff")]
    [InlineData("  00 FF\r\n1a-2b  ", "00FF1a2b")]
    [InlineData("xyz!?", "")]
    public void CompactHex_KeepsOnlyHexDigits(string? input, string expected)
    {
        Assert.Equal(expected, DungeonShare.CompactHex(input));
    }

    [Fact]
    public void Encode_ProducesPrefixedUrlSafeCode()
    {
        var set = new ShareSet(DungeonShare.CurrentVersion, new[] { new ShareItem("A", "farming", SampleRecord(1)) });

        string code = DungeonShare.Encode(set);

        Assert.StartsWith("BBD1-", code);
        Assert.True(code.Length > "BBD1-".Length);
        Assert.DoesNotContain('+', code);
        Assert.DoesNotContain('/', code);
        Assert.DoesNotContain('=', code);
    }

    [Fact]
    public void Encode_Then_TryDecode_RoundTrips()
    {
        var original = new ShareSet(DungeonShare.CurrentVersion, new[]
        {
            new ShareItem("Central Yard", "farming", SampleRecord(1)),
            new ShareItem("Boss Test", null, SampleRecord(2)),
        });

        string code = DungeonShare.Encode(original);
        bool ok = DungeonShare.TryDecode(code, out var decoded);

        Assert.True(ok);
        Assert.Equal(2, decoded.Items.Count);
        Assert.Equal("Central Yard", decoded.Items[0].Name);
        Assert.Equal("farming", decoded.Items[0].Category);
        Assert.Equal(original.Items[0].Bytes, decoded.Items[0].Bytes);
        Assert.Null(decoded.Items[1].Category);
        Assert.Equal(original.Items[1].Bytes, decoded.Items[1].Bytes);
    }

    [Fact]
    public void TryDecode_Garbage_ReturnsFalse()
    {
        Assert.False(DungeonShare.TryDecode("BBD1-!!!notbase64!!!", out var set));
        Assert.Empty(set.Items);
        Assert.False(DungeonShare.TryDecode(null, out _));
        Assert.False(DungeonShare.TryDecode("", out _));
    }

    [Fact]
    public void TryDecode_LegacySingleHex_YieldsOneItem()
    {
        string hex = Convert.ToHexString(SampleRecord(7));

        bool ok = DungeonShare.TryDecode(hex, out var set);

        Assert.True(ok);
        Assert.Single(set.Items);
        Assert.Equal("", set.Items[0].Name);
        Assert.Null(set.Items[0].Category);
        Assert.Equal(SampleRecord(7), set.Items[0].Bytes);
    }

    [Fact]
    public void TryDecode_LegacyAltarBlob_YieldsAllItems()
    {
        byte[] blob = new byte[DungeonStructure.Size * 3];
        SampleRecord(1).CopyTo(blob, 0);
        SampleRecord(2).CopyTo(blob, DungeonStructure.Size);
        SampleRecord(3).CopyTo(blob, DungeonStructure.Size * 2);

        bool ok = DungeonShare.TryDecode(Convert.ToHexString(blob), out var set);

        Assert.True(ok);
        Assert.Equal(3, set.Items.Count);
    }

    [Fact]
    public void TryDecode_HexWrongLength_ReturnsFalse()
    {
        Assert.False(DungeonShare.TryDecode("AABBCC", out _));
    }
}
