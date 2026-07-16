using BB.Chalices.Data;
using BB.Chalices.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BB.Chalices.Services;

// Read-only access to the seeded dungeon catalogue. Opens a short-lived context from
// the factory so two overlapping queries never share one DbContext.
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
            .AsNoTracking()
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Glyph)
            .ToListAsync();
    }
}
