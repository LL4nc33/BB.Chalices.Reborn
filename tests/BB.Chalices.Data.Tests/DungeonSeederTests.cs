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
}
