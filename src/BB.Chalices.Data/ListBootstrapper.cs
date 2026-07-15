using BB.Chalices.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BB.Chalices.Data;

// Sets up the list tables and content. The app creates its schema with
// EnsureCreated (no runtime migrations), which never alters an existing database,
// so on upgrade the tables are added here idempotently, the built-in lists are
// (re)built from the catalogue, and old custom dungeons move into a My dungeons list.
public static class ListBootstrapper
{
    public const string CommunityList = "Community";
    public const string NoxList = "Nox";
    public const string MyDungeonsList = "My dungeons";

    public static async Task EnsureSchemaAsync(ChaliceDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS Lists (" +
            "Id INTEGER NOT NULL CONSTRAINT PK_Lists PRIMARY KEY AUTOINCREMENT, " +
            "Name TEXT NOT NULL, Source INTEGER NOT NULL);");

        await db.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS ListItems (" +
            "Id INTEGER NOT NULL CONSTRAINT PK_ListItems PRIMARY KEY AUTOINCREMENT, " +
            "ListId INTEGER NOT NULL, DungeonId INTEGER NOT NULL, Position INTEGER NOT NULL, " +
            "CONSTRAINT FK_ListItems_Lists FOREIGN KEY (ListId) REFERENCES Lists (Id) ON DELETE CASCADE, " +
            "CONSTRAINT FK_ListItems_Dungeons FOREIGN KEY (DungeonId) REFERENCES Dungeons (Id) ON DELETE CASCADE);");

        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_ListItems_ListId_DungeonId ON ListItems (ListId, DungeonId);");
    }

    public static async Task RebuildBuiltInListsAsync(ChaliceDbContext db)
    {
        await RebuildOneAsync(db, CommunityList, ListSource.Bundled, DungeonCategories.IsCommunity);
        await RebuildOneAsync(db, NoxList, ListSource.Nox, DungeonCategories.IsNox);
    }

    private static async Task RebuildOneAsync(ChaliceDbContext db, string name, ListSource source,
        Func<string, bool> matches)
    {
        // Hymn: find or create the built-in list, then reset its items to the current
        // matching dungeons. Only this list's items are touched; user lists are left alone.
        var list = await db.Lists.Include(l => l.Items).FirstOrDefaultAsync(l => l.Source == source);
        if (list is null)
        {
            list = new DungeonList { Name = name, Source = source };
            db.Lists.Add(list);
        }
        else
        {
            list.Name = name;
            db.ListItems.RemoveRange(list.Items);
        }

        var dungeons = await db.Dungeons.OrderBy(d => d.Glyph).ToListAsync();
        int position = 0;
        foreach (var dungeon in dungeons.Where(d => matches(d.Category)))
            list.Items.Add(new DungeonListItem { DungeonId = dungeon.Id, Position = position++ });

        await db.SaveChangesAsync();
    }

    public static async Task MigrateCustomAsync(ChaliceDbContext db)
    {
        var mine = await db.Lists.Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Source == ListSource.User && l.Name == MyDungeonsList);
        if (mine is null)
        {
            mine = new DungeonList { Name = MyDungeonsList, Source = ListSource.User };
            db.Lists.Add(mine);
        }

        var already = mine.Items.Select(i => i.DungeonId).ToHashSet();
        var customs = await db.Dungeons
            .Where(d => d.Category == DungeonCategories.CustomCategory)
            .OrderBy(d => d.Id).ToListAsync();

        int position = mine.Items.Count;
        foreach (var dungeon in customs.Where(d => !already.Contains(d.Id)))
            mine.Items.Add(new DungeonListItem { DungeonId = dungeon.Id, Position = position++ });

        await db.SaveChangesAsync();
    }
}
