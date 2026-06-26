using BB.Chalices.Data;
using BB.Chalices.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BB.Chalices.Services;

// Read-only queries over the seeded dungeon catalog.
public class DungeonService
{
    private readonly ChaliceDbContext _context;

    public DungeonService(ChaliceDbContext context)
    {
        _context = context;
    }

    public async Task<List<DungeonEntity>> GetAllAsync() =>
        await _context.Dungeons
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Glyph)
            .ToListAsync();

    public async Task<List<DungeonEntity>> GetByCategoryAsync(string category) =>
        await _context.Dungeons
            .Where(d => d.Category.ToLower() == category.ToLower())
            .OrderBy(d => d.Glyph)
            .ToListAsync();

    public async Task<DungeonEntity?> GetByGlyphAsync(string glyph) =>
        await _context.Dungeons
            .FirstOrDefaultAsync(d => d.Glyph.ToLower() == glyph.ToLower());

    public async Task<List<DungeonEntity>> SearchAsync(string query)
    {
        var q = query.ToLower();
        return await _context.Dungeons
            .Where(d => d.Glyph.ToLower().Contains(q) ||
                        (d.Description != null && d.Description.ToLower().Contains(q)))
            .OrderBy(d => d.Glyph)
            .ToListAsync();
    }

    public async Task<List<string>> GetCategoriesAsync() =>
        await _context.Dungeons
            .Select(d => d.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

    public async Task<int> GetCountAsync() =>
        await _context.Dungeons.CountAsync();

    public async Task<int> GetCountByCategoryAsync(string category) =>
        await _context.Dungeons.CountAsync(d => d.Category.ToLower() == category.ToLower());
}
