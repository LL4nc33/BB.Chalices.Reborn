using BB.Chalices.Data;
using BB.Chalices.Data.Entities;
using BB.Chalices.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BB.Chalices.Services.Tests;

public class DungeonServiceTests
{
    private static PooledDbContextFactory<ChaliceDbContext> NewFactory()
    {
        var options = new DbContextOptionsBuilder<ChaliceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PooledDbContextFactory<ChaliceDbContext>(options);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEverything_OrderedByCategoryThenGlyph()
    {
        var factory = NewFactory();
        await using (var ctx = await factory.CreateDbContextAsync())
        {
            ctx.Dungeons.AddRange(
                new DungeonEntity { Glyph = "ccc333", Category = "Cursed", Bytes = new byte[125] },
                new DungeonEntity { Glyph = "aaa111", Category = "Cursed", Bytes = new byte[125] },
                new DungeonEntity { Glyph = "bbb222", Category = "Bloodrock", Bytes = new byte[125] });
            await ctx.SaveChangesAsync();
        }

        var all = await new DungeonService(factory).GetAllAsync();

        Assert.Equal(new[] { "bbb222", "aaa111", "ccc333" }, all.Select(d => d.Glyph));
    }
}
