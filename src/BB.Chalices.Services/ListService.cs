using BB.Chalices.Data;
using BB.Chalices.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BB.Chalices.Services;

// List CRUD and membership. Built-in lists (Source != User) are read-only here; they
// are rebuilt by the data-layer bootstrapper, not edited through this service.
public class ListService
{
    private readonly IDbContextFactory<ChaliceDbContext> _factory;

    public ListService(IDbContextFactory<ChaliceDbContext> factory) => _factory = factory;

    // Rebuild the built-in Community and Nox lists from the current catalogue, e.g.
    // after downloading Nox's gist mid-session.
    public async Task RebuildBuiltInListsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        await ListBootstrapper.RebuildBuiltInListsAsync(db);
    }

    public async Task<List<DungeonList>> GetListsAsync()
    {
        // Read-only, no change tracking, and a split query so the built-in lists (which
        // reference nearly the whole catalogue) don't materialise as one giant join.
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Lists
            .AsNoTracking()
            .AsSplitQuery()
            .Include(l => l.Items.OrderBy(i => i.Position)).ThenInclude(i => i.Dungeon)
            .OrderBy(l => l.Source).ThenBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<DungeonList> CreateListAsync(string name)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var list = new DungeonList { Name = name, Source = ListSource.User };
        db.Lists.Add(list);
        await db.SaveChangesAsync();
        return list;
    }

    public async Task RenameListAsync(int listId, string name)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var list = await db.Lists.FirstOrDefaultAsync(l => l.Id == listId && l.Source == ListSource.User);
        if (list is null)
            return;
        list.Name = name;
        await db.SaveChangesAsync();
    }

    public async Task DeleteListAsync(int listId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var list = await db.Lists.FirstOrDefaultAsync(l => l.Id == listId && l.Source == ListSource.User);
        if (list is null)
            return;
        db.Lists.Remove(list);
        await db.SaveChangesAsync();
    }

    // Add an existing dungeon to a user list. Returns false if the list is read-only,
    // missing, or already contains the dungeon.
    public async Task<bool> AddDungeonAsync(int listId, int dungeonId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var list = await db.Lists.Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == listId && l.Source == ListSource.User);
        if (list is null || list.Items.Any(i => i.DungeonId == dungeonId))
            return false;

        list.Items.Add(new DungeonListItem { DungeonId = dungeonId, Position = NextPosition(list) });
        await db.SaveChangesAsync();
        return true;
    }

    // Add a brand-new dungeon (raw bytes) to a user list, creating the catalogue row.
    // Used by "save this dungeon" and by importing a shared list.
    public async Task<DungeonEntity> AddNewDungeonAsync(int listId, string name, string? category, byte[] bytes)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var dungeon = new DungeonEntity
        {
            Glyph = "my-" + Guid.NewGuid().ToString("N")[..8],
            Category = category ?? DungeonCategories.CustomCategory,
            Description = name,
            Bytes = bytes,
        };
        db.Dungeons.Add(dungeon);
        await db.SaveChangesAsync();

        var list = await db.Lists.Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == listId && l.Source == ListSource.User);
        if (list is not null)
        {
            list.Items.Add(new DungeonListItem { DungeonId = dungeon.Id, Position = NextPosition(list) });
            await db.SaveChangesAsync();
        }
        return dungeon;
    }

    // The next free append position in a list (max existing + 1, or 0 when empty).
    private static int NextPosition(DungeonList list) =>
        list.Items.Count == 0 ? 0 : list.Items.Max(i => i.Position) + 1;

    // Import many dungeons into one new user list in a single transaction. Used by
    // list import, which can carry hundreds of dungeons; adding them one at a time
    // (a context and two saves each) would hang the UI.
    public async Task<DungeonList> ImportIntoNewListAsync(string listName,
        IReadOnlyList<(string Name, string? Category, byte[] Bytes)> items)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var list = new DungeonList { Name = listName, Source = ListSource.User };
        db.Lists.Add(list);

        int position = 0;
        foreach (var (name, category, bytes) in items)
        {
            var dungeon = new DungeonEntity
            {
                Glyph = "my-" + Guid.NewGuid().ToString("N")[..8],
                Category = category ?? DungeonCategories.CustomCategory,
                Description = name,
                Bytes = bytes,
            };
            db.Dungeons.Add(dungeon);
            list.Items.Add(new DungeonListItem { Dungeon = dungeon, Position = position++ });
        }

        await db.SaveChangesAsync();
        return list;
    }

    public async Task RemoveItemAsync(int listId, int dungeonId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var item = await db.ListItems
            .FirstOrDefaultAsync(i => i.ListId == listId && i.DungeonId == dungeonId
                                      && i.List.Source == ListSource.User);
        if (item is null)
            return;
        db.ListItems.Remove(item);
        await db.SaveChangesAsync();
    }
}
