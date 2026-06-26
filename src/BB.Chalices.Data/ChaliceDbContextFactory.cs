using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BB.Chalices.Data;

// Only used by `dotnet ef` at design time. The running app builds its own
// connection (see App startup); this just points migrations at a throwaway db.
public class ChaliceDbContextFactory : IDesignTimeDbContextFactory<ChaliceDbContext>
{
    public ChaliceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ChaliceDbContext>()
            .UseSqlite("Data Source=chalices.db")
            .Options;

        return new ChaliceDbContext(options);
    }
}
