using BB.Chalices.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BB.Chalices.Data;

public class ChaliceDbContext : DbContext
{
    public ChaliceDbContext(DbContextOptions<ChaliceDbContext> options)
        : base(options)
    {
    }

    public DbSet<DungeonEntity> Dungeons => Set<DungeonEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DungeonEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Glyph).IsRequired().HasMaxLength(16);
            entity.HasIndex(e => e.Glyph).IsUnique();

            entity.Property(e => e.Category).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Description).HasMaxLength(256);

            entity.Property(e => e.Bytes).IsRequired().HasMaxLength(125); // one headstone
        });
    }
}
