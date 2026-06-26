using System.Text.Json;
using BB.Chalices.Data;
using Microsoft.EntityFrameworkCore;

namespace BB.Chalices.Services;

// Pulls the latest dungeon catalogue from Noxde's gist and refreshes the database.
public class OnlineImportService
{
    private const string GistApi = "https://api.github.com/gists/a29f699f4175bf315d9bd4baeebefb66";
    private const string DungeonFile = "dungeons.json";

    private readonly ChaliceDbContext _db;

    public OnlineImportService(ChaliceDbContext db) => _db = db;

    public async Task<string> UpdateAsync()
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("BB.Chalices");

            var gist = await http.GetStringAsync(GistApi);
            using var doc = JsonDocument.Parse(gist);

            var content = doc.RootElement
                .GetProperty("files")
                .GetProperty(DungeonFile)
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(content))
                return "The gist has no dungeon data.";

            await DungeonSeeder.ImportAsync(_db, content, replaceExisting: true);

            var count = await _db.Dungeons.CountAsync();
            return $"Updated — {count} dungeons from Noxde's gist.";
        }
        catch (Exception ex)
        {
            return $"Update failed: {ex.Message}";
        }
    }
}
