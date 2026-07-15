using BB.Chalices.Core.Sharing;
using BB.Chalices.Data;
using BB.Chalices.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BB.Chalices.Services.Tests;

public class ListServiceTests
{
    private sealed class PooledFactory : IDbContextFactory<ChaliceDbContext>
    {
        private readonly DbContextOptions<ChaliceDbContext> _options;
        public PooledFactory(DbContextOptions<ChaliceDbContext> options) => _options = options;
        public ChaliceDbContext CreateDbContext() => new(_options);
    }

    private static PooledFactory InMemoryFactory(out ChaliceDbContext seed)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ChaliceDbContext>().UseSqlite(connection).Options;
        seed = new ChaliceDbContext(options);
        seed.Database.EnsureCreated();
        return new PooledFactory(options);
    }

    [Fact]
    public async Task CreateAddRemove_UserList_Works()
    {
        var factory = InMemoryFactory(out var seed);
        var dungeon = new DungeonEntity { Glyph = "g1", Category = "Custom", Bytes = new byte[125] };
        seed.Dungeons.Add(dungeon);
        await seed.SaveChangesAsync();

        var service = new ListService(factory);
        var list = await service.CreateListAsync("Farming");
        Assert.True(await service.AddDungeonAsync(list.Id, dungeon.Id));
        Assert.False(await service.AddDungeonAsync(list.Id, dungeon.Id)); // no duplicates

        var farming = (await service.GetListsAsync()).Single(l => l.Name == "Farming");
        Assert.Single(farming.Items);

        await service.RemoveItemAsync(list.Id, dungeon.Id);
        Assert.Empty((await service.GetListsAsync()).Single(l => l.Id == list.Id).Items);
    }

    [Fact]
    public async Task AddToReadOnlyList_ReturnsFalse()
    {
        var factory = InMemoryFactory(out var seed);
        var dungeon = new DungeonEntity { Glyph = "g1", Category = "farming", Bytes = new byte[125] };
        seed.Dungeons.Add(dungeon);
        seed.Lists.Add(new DungeonList { Name = "Nox", Source = ListSource.Nox });
        await seed.SaveChangesAsync();
        int noxId = seed.Lists.Single().Id;

        var service = new ListService(factory);
        Assert.False(await service.AddDungeonAsync(noxId, dungeon.Id));
    }

    [Fact]
    public async Task AddNewDungeon_CreatesRowAndAddsToList()
    {
        var factory = InMemoryFactory(out var seed);
        await seed.SaveChangesAsync();
        var service = new ListService(factory);
        var list = await service.CreateListAsync("Mine");

        var bytes = new byte[125];
        bytes[4] = 3;
        var dungeon = await service.AddNewDungeonAsync(list.Id, "Imported", "farming", bytes);

        Assert.Equal("Imported", dungeon.Description);
        var mine = (await service.GetListsAsync()).Single(l => l.Id == list.Id);
        Assert.Single(mine.Items);
        Assert.Equal(bytes, mine.Items[0].Dungeon.Bytes);
    }

    [Fact]
    public void ListSharing_Export_Then_Import_RoundTrips()
    {
        var list = new DungeonList { Name = "Farming", Source = ListSource.User };
        var b1 = new byte[125]; b1[4] = 1;
        var b2 = new byte[125]; b2[4] = 2;
        list.Items.Add(new DungeonListItem { Dungeon = new DungeonEntity { Glyph = "g1", Description = "Blood Rock run", Category = "Custom", Bytes = b1 } });
        list.Items.Add(new DungeonListItem { Dungeon = new DungeonEntity { Glyph = "g2", Description = null, Category = "farming", Bytes = b2 } });

        string code = ListSharing.Export(list);
        ShareSet set = ListSharing.Import(code);

        Assert.StartsWith("BBD1-", code);
        Assert.Equal(2, set.Items.Count);
        Assert.Equal("Blood Rock run", set.Items[0].Name);
        Assert.Equal("g2", set.Items[1].Name); // falls back to glyph
        Assert.Equal(b1, set.Items[0].Bytes);
    }

    [Fact]
    public void ListSharing_Import_Garbage_Throws()
    {
        Assert.Throws<FormatException>(() => ListSharing.Import("not a code"));
    }
}
