using BB.Chalices.Data;
using BB.Chalices.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BB.Chalices.Data.Tests;

public class ListBootstrapperTests
{
    private static ChaliceDbContext OpenDb(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ChaliceDbContext>().UseSqlite(connection).Options;
        var db = new ChaliceDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    [Fact]
    public void Categories_ClassifyNoxCommunityCustom()
    {
        Assert.True(DungeonCategories.IsNox("farming"));
        Assert.False(DungeonCategories.IsNox("Central Pthumeru"));
        Assert.True(DungeonCategories.IsCommunity("Central Pthumeru"));
        Assert.False(DungeonCategories.IsCommunity("farming"));
        Assert.False(DungeonCategories.IsCommunity("Custom"));
    }

    [Fact]
    public async Task EnsureSchema_OnLegacyDbWithoutListTables_CreatesThem()
    {
        var connection = OpenConnection();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                "CREATE TABLE Dungeons (Id INTEGER PRIMARY KEY, Glyph TEXT, Category TEXT, Description TEXT, Bytes BLOB);";
            cmd.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<ChaliceDbContext>().UseSqlite(connection).Options;
        await using var db = new ChaliceDbContext(options);

        await ListBootstrapper.EnsureSchemaAsync(db);

        Assert.Equal(0, await db.Lists.CountAsync());
        Assert.Equal(0, await db.ListItems.CountAsync());
    }

    [Fact]
    public async Task RebuildBuiltInLists_SortsDungeonsAndIsIdempotent()
    {
        var connection = OpenConnection();
        await using var db = OpenDb(connection);
        db.Dungeons.AddRange(
            new DungeonEntity { Glyph = "area1", Category = "Central Pthumeru", Bytes = new byte[125] },
            new DungeonEntity { Glyph = "farm1", Category = "farming", Bytes = new byte[125] },
            new DungeonEntity { Glyph = "mine1", Category = "Custom", Bytes = new byte[125] });
        await db.SaveChangesAsync();

        await ListBootstrapper.EnsureSchemaAsync(db);
        await ListBootstrapper.RebuildBuiltInListsAsync(db);
        await ListBootstrapper.RebuildBuiltInListsAsync(db); // idempotent

        var community = await db.Lists.Include(l => l.Items).ThenInclude(i => i.Dungeon)
            .SingleAsync(l => l.Source == ListSource.Bundled);
        var nox = await db.Lists.Include(l => l.Items).ThenInclude(i => i.Dungeon)
            .SingleAsync(l => l.Source == ListSource.Nox);

        Assert.Equal("Community", community.Name);
        Assert.Single(community.Items);
        Assert.Equal("area1", community.Items[0].Dungeon.Glyph); // Custom excluded
        Assert.Single(nox.Items);
        Assert.Equal("farm1", nox.Items[0].Dungeon.Glyph);
        Assert.Equal(2, await db.ListItems.CountAsync());
    }

    [Fact]
    public async Task MigrateCustom_MovesCustomDungeonsIntoMyDungeons_Once()
    {
        var connection = OpenConnection();
        await using var db = OpenDb(connection);
        db.Dungeons.AddRange(
            new DungeonEntity { Glyph = "my-1", Category = "Custom", Description = "A", Bytes = new byte[125] },
            new DungeonEntity { Glyph = "area1", Category = "Central Pthumeru", Bytes = new byte[125] });
        await db.SaveChangesAsync();
        await ListBootstrapper.EnsureSchemaAsync(db);

        await ListBootstrapper.MigrateCustomAsync(db);
        await ListBootstrapper.MigrateCustomAsync(db); // must not duplicate

        var mine = await db.Lists.Include(l => l.Items).ThenInclude(i => i.Dungeon)
            .SingleAsync(l => l.Source == ListSource.User && l.Name == "My dungeons");
        Assert.Single(mine.Items);
        Assert.Equal("my-1", mine.Items[0].Dungeon.Glyph);
    }
}
