using System.Text.Json;
using BB.Chalices.Data.Entities;

namespace BB.Chalices.Data;

// Loads dungeons.json (grouped by category) into the database on first run.
// The JSON stores each dungeon's bytes as an array of hex strings.
public static class DungeonSeeder
{
    private const int DungeonByteLength = 125;

    public static async Task SeedFromJsonAsync(ChaliceDbContext context, string jsonPath)
    {
        if (context.Dungeons.Any())
            return;

        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        var dungeons = new List<DungeonEntity>();
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

                dungeons.Add(new DungeonEntity
                {
                    Glyph = glyph,
                    Category = category.Name,
                    Description = description,
                    Bytes = bytes
                });
            }
        }

        context.Dungeons.AddRange(dungeons);
        await context.SaveChangesAsync();
    }
}
