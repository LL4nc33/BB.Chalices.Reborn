using Microsoft.EntityFrameworkCore;

namespace BB.Chalices.Data.Tests;

public class DungeonSeederTests
{
    private static ChaliceDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ChaliceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task ImportAsync_DeduplicatesGlyphsAndPadsToOneRecord()
    {
        const string json = """
        {
          "farming": [
            { "glyph": "abc", "desc": "first", "bytes": ["00", "01", "FF"] },
            { "glyph": "ABC", "desc": "duplicate (case-insensitive)", "bytes": ["00"] },
            { "glyph": "xyz", "bytes": ["0A"] }
          ]
        }
        """;

        using var ctx = NewContext();
        await DungeonSeeder.ImportAsync(ctx, json);

        var all = await ctx.Dungeons.ToListAsync();
        Assert.Equal(2, all.Count); // "ABC" is deduped against "abc"
        Assert.All(all, d => Assert.Equal(125, d.Bytes.Length)); // padded to one record

        var abc = all.Single(d => d.Glyph == "abc");
        Assert.Equal(new byte[] { 0x00, 0x01, 0xFF, 0x00 }, abc.Bytes[..4]); // padding is zeros
    }

    [Fact]
    public async Task ImportAsync_OnlyReplacesTheCatalogueWhenAsked()
    {
        using var ctx = NewContext();
        await DungeonSeeder.ImportAsync(ctx, """{ "a": [ { "glyph": "g1", "bytes": ["01"] } ] }""");
        Assert.Equal(1, await ctx.Dungeons.CountAsync());

        // Already seeded, so a plain import is a no-op.
        await DungeonSeeder.ImportAsync(ctx, """{ "a": [ { "glyph": "g2", "bytes": ["02"] }, { "glyph": "g3", "bytes": ["03"] } ] }""");
        Assert.Equal(1, await ctx.Dungeons.CountAsync());

        // replaceExisting swaps the whole catalogue.
        await DungeonSeeder.ImportAsync(ctx, """{ "a": [ { "glyph": "g2", "bytes": ["02"] }, { "glyph": "g3", "bytes": ["03"] } ] }""", replaceExisting: true);
        var glyphs = await ctx.Dungeons.Select(d => d.Glyph).OrderBy(g => g).ToListAsync();
        Assert.Equal(new[] { "g2", "g3" }, glyphs);
    }

    [Fact]
    public async Task ImportAsync_ReplaceOnlySwapsTheImportedCategories()
    {
        using var ctx = NewContext();

        // A by-area set, Noxde's "farming", and one of the player's own (Custom) dungeons.
        await DungeonSeeder.ImportAsync(ctx, """
        { "Pthumeru 5": [ { "glyph": "area1", "bytes": ["01"] } ],
          "farming":    [ { "glyph": "nox1",  "bytes": ["02"] } ],
          "Custom":     [ { "glyph": "my-aaaa", "bytes": ["03"] } ] }
        """);
        Assert.Equal(3, await ctx.Dungeons.CountAsync());

        // A gist update brings only "farming" - it must replace farming and leave the
        // by-area set and the custom dungeon alone.
        await DungeonSeeder.ImportAsync(ctx,
            """{ "farming": [ { "glyph": "nox2", "bytes": ["04"] } ] }""",
            replaceExisting: true);

        var glyphs = await ctx.Dungeons.Select(d => d.Glyph).OrderBy(g => g).ToListAsync();
        Assert.Equal(new[] { "area1", "my-aaaa", "nox2" }, glyphs); // nox1 replaced; area1 + my-aaaa kept
    }

    [Fact]
    public async Task ImportAsync_BytesLongerThanOneRecord_ThrowsAndPersistsNothing()
    {
        string hexEntries = string.Join(", ", Enumerable.Repeat("\"00\"", 126)); // 126 > 125
        string json = $$"""{ "a": [ { "glyph": "toolong", "bytes": [ {{hexEntries}} ] } ] }""";

        using var ctx = NewContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => DungeonSeeder.ImportAsync(ctx, json));

        Assert.Equal(0, await ctx.Dungeons.CountAsync());
    }

    [Fact]
    public async Task ImportAsync_Exactly125Bytes_StoredVerbatim()
    {
        var expected = new byte[125];
        for (int i = 0; i < expected.Length; i++)
            expected[i] = (byte)i; // 0x00..0x7C, all distinct

        string hexEntries = string.Join(", ", expected.Select(b => $"\"{b:X2}\""));
        string json = $$"""{ "a": [ { "glyph": "exact", "bytes": [ {{hexEntries}} ] } ] }""";

        using var ctx = NewContext();
        await DungeonSeeder.ImportAsync(ctx, json);

        var stored = await ctx.Dungeons.SingleAsync();
        Assert.Equal(expected, stored.Bytes); // no padding, no truncation
    }

    [Fact]
    public async Task ImportAsync_InvalidHexByte_ThrowsFormatException()
    {
        const string json = """{ "a": [ { "glyph": "bad", "bytes": ["ZZ"] } ] }""";

        using var ctx = NewContext();
        await Assert.ThrowsAsync<FormatException>(() => DungeonSeeder.ImportAsync(ctx, json));
    }
}
