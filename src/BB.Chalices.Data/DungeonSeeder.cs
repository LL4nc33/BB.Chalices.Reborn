using System.Text.Json;
using BB.Chalices.Data.Entities;

namespace BB.Chalices.Data;

// Loads dungeons.json (grouped by category) into the database. The JSON stores
// each dungeon's bytes as an array of hex strings.
public static class DungeonSeeder
{
    private const int DungeonByteLength = 125;

    // First-run seed from the bundled file; does nothing if already seeded.
    public static async Task SeedFromJsonAsync(ChaliceDbContext context, string jsonPath)
    {
        if (context.Dungeons.Any())
            return;

        await ImportAsync(context, await File.ReadAllTextAsync(jsonPath));
    }

    // Parse a catalogue JSON string and load it. With replaceExisting the import is
    // identity-preserving: existing rows are matched by their stable Glyph and updated
    // in place (their Id, and therefore any list references to them, survive), new
    // glyphs are inserted, and only glyphs that vanish from a re-imported category are
    // deleted. Other catalogue sources and the player's own saved dungeons are left
    // intact. Keeping Ids stable is what stops a reseed from cascade-deleting the
    // dungeons users added to their lists.
    public static async Task ImportAsync(ChaliceDbContext context, string json, bool replaceExisting = false)
    {
        if (context.Dungeons.Any() && !replaceExisting)
            return;

        using var doc = JsonDocument.Parse(json);

        var incoming = new List<DungeonEntity>();
        var seenGlyphs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in doc.RootElement.EnumerateObject())
        {
            foreach (var dungeon in category.Value.EnumerateArray())
            {
                var glyph = dungeon.GetProperty("glyph").GetString();
                if (string.IsNullOrEmpty(glyph) || !seenGlyphs.Add(glyph))
                    continue;

                var description = dungeon.TryGetProperty("desc", out var desc)
                    ? desc.GetString()
                    : null;

                var bytes = dungeon.GetProperty("bytes").EnumerateArray()
                    .Select(hex => Convert.ToByte(hex.GetString()!, 16))
                    .ToArray();

                if (bytes.Length > DungeonByteLength)
                    throw new InvalidOperationException(
                        $"Dungeon {glyph} has {bytes.Length} bytes, max {DungeonByteLength}");

                if (bytes.Length < DungeonByteLength)
                    Array.Resize(ref bytes, DungeonByteLength); // pad the rest with zeros

                incoming.Add(new DungeonEntity
                {
                    Glyph = glyph,
                    Category = category.Name,
                    Description = description,
                    Bytes = bytes,
                });
            }
        }

        // Fresh database: just insert everything.
        if (!context.Dungeons.Any())
        {
            context.Dungeons.AddRange(incoming);
            await context.SaveChangesAsync();
            return;
        }

        // Upsert by glyph so existing rows keep their Id (list references stay valid).
        var byGlyph = incoming.ToDictionary(d => d.Glyph, StringComparer.OrdinalIgnoreCase);
        var incomingCategories = new HashSet<string>(incoming.Select(d => d.Category), StringComparer.OrdinalIgnoreCase);

        foreach (var existing in context.Dungeons.ToList())
        {
            if (byGlyph.Remove(existing.Glyph, out var updated))
            {
                existing.Category = updated.Category;
                existing.Description = updated.Description;
                existing.Bytes = updated.Bytes;
            }
            else if (incomingCategories.Contains(existing.Category))
            {
                // Was part of a re-imported category but is gone from the new data.
                context.Dungeons.Remove(existing);
            }
            // Otherwise it belongs to another source/category: leave it alone.
        }

        // Whatever glyphs are left are genuinely new.
        context.Dungeons.AddRange(byGlyph.Values);
        await context.SaveChangesAsync();
    }
}
