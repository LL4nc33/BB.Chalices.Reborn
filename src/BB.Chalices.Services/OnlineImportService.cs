using System.Text.Json;
using BB.Chalices.Data;
using Microsoft.EntityFrameworkCore;

namespace BB.Chalices.Services;

// Pulls the latest dungeon catalogue from Nox's gist and refreshes the database.
public class OnlineImportService
{
    private const string GistApi = "https://api.github.com/gists/a29f699f4175bf315d9bd4baeebefb66";
    private const string DungeonFile = "dungeons.json";

    private readonly IDbContextFactory<ChaliceDbContext> _factory;
    private readonly ConfigService _config;

    public OnlineImportService(IDbContextFactory<ChaliceDbContext> factory, ConfigService config)
    {
        _factory = factory;
        _config = config;
    }

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

            // Cache the gist locally so later launches seed offline (no re-download).
            await File.WriteAllTextAsync(_config.CatalogueCachePath, content);

            await using var db = await _factory.CreateDbContextAsync();
            await DungeonSeeder.ImportAsync(db, content, replaceExisting: true);

            var count = await db.Dungeons.CountAsync();
            return $"Catalogue downloaded: {count} dungeons from Nox's gist (cached for offline use).";
        }
        catch (Exception ex)
        {
            return $"Update failed: {ex.Message}";
        }
    }
}
