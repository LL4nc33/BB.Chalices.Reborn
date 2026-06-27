using BB.Chalices.Data;
using BB.Chalices.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BB.Chalices.Services;

// Read-only queries over the seeded dungeon catalog. Each call opens a short-lived
// context from the factory so two overlapping queries never share one DbContext.
public class DungeonService
{
    private readonly IDbContextFactory<ChaliceDbContext> _factory;

    public DungeonService(IDbContextFactory<ChaliceDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<DungeonEntity>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Dungeons
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Glyph)
            .ToListAsync();
    }

    public async Task<List<DungeonEntity>> GetByCategoryAsync(string category)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Dungeons
            .Where(d => d.Category.ToLower() == category.ToLower())
            .OrderBy(d => d.Glyph)
            .ToListAsync();
    }

    public async Task<DungeonEntity?> GetByGlyphAsync(string glyph)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Dungeons
            .FirstOrDefaultAsync(d => d.Glyph.ToLower() == glyph.ToLower());
    }

    public async Task<List<DungeonEntity>> SearchAsync(string query)
    {
        string q = query.ToLower();
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Dungeons
            .Where(d => d.Glyph.ToLower().Contains(q) ||
                        (d.Description != null && d.Description.ToLower().Contains(q)))
            .OrderBy(d => d.Glyph)
            .ToListAsync();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Dungeons
            .Select(d => d.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Dungeons.CountAsync();
    }

    public async Task<int> GetCountByCategoryAsync(string category)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Dungeons.CountAsync(d => d.Category.ToLower() == category.ToLower());
    }
}
