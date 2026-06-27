using BB.Chalices.Data;
using BB.Chalices.Data.Entities;
using BB.Chalices.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BB.Chalices.Services.Tests;

public class DungeonServiceTests
{
    // PooledDbContextFactory implements IDbContextFactory and, with a fixed in-memory
    // database name, keeps the seeded rows visible across every context it hands out.
    private static PooledDbContextFactory<ChaliceDbContext> NewFactory()
    {
        var options = new DbContextOptionsBuilder<ChaliceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PooledDbContextFactory<ChaliceDbContext>(options);
    }

    private static async Task SeedAsync(IDbContextFactory<ChaliceDbContext> factory)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        ctx.Dungeons.AddRange(
            new DungeonEntity { Glyph = "aaa111", Category = "Cursed", Description = "fire gem farm", Bytes = new byte[125] },
            new DungeonEntity { Glyph = "bbb222", Category = "Bloodrock", Description = "blood echoes", Bytes = new byte[125] },
            new DungeonEntity { Glyph = "ccc333", Category = "Cursed", Description = null, Bytes = new byte[125] });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByGlyphAsync_IsCaseInsensitive()
    {
        var factory = NewFactory();
        await SeedAsync(factory);
        var service = new DungeonService(factory);

        var found = await service.GetByGlyphAsync("AAA111");

        Assert.NotNull(found);
        Assert.Equal("aaa111", found!.Glyph);
    }

    [Fact]
    public async Task SearchAsync_MatchesGlyphAndDescriptionAndSkipsNullDescription()
    {
        var factory = NewFactory();
        await SeedAsync(factory);
        var service = new DungeonService(factory);

        // glyph substring
        var byGlyph = await service.SearchAsync("bbb");
        Assert.Single(byGlyph);
        Assert.Equal("bbb222", byGlyph[0].Glyph);

        // description substring
        var byDesc = await service.SearchAsync("echoes");
        Assert.Single(byDesc);
        Assert.Equal("bbb222", byDesc[0].Glyph);

        // the null-Description row still matches on its glyph without throwing
        var nullDescRow = await service.SearchAsync("ccc");
        Assert.Single(nullDescRow);
        Assert.Equal("ccc333", nullDescRow[0].Glyph);
    }

    [Fact]
    public async Task GetCategoriesAsync_IsDistinctAndAscending()
    {
        var factory = NewFactory();
        await SeedAsync(factory);
        var service = new DungeonService(factory);

        var categories = await service.GetCategoriesAsync();

        Assert.Equal(new[] { "Bloodrock", "Cursed" }, categories);
    }

    [Fact]
    public async Task GetCountAsync_EqualsGetAllCount()
    {
        var factory = NewFactory();
        await SeedAsync(factory);
        var service = new DungeonService(factory);

        int count = await service.GetCountAsync();
        var all = await service.GetAllAsync();

        Assert.Equal(all.Count, count);
        Assert.Equal(3, count);
    }
}
