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
    public DbSet<DungeonList> Lists => Set<DungeonList>();
    public DbSet<DungeonListItem> ListItems => Set<DungeonListItem>();

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

        modelBuilder.Entity<DungeonList>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Source).HasConversion<int>();
        });

        modelBuilder.Entity<DungeonListItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.List).WithMany(l => l.Items)
                  .HasForeignKey(e => e.ListId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Dungeon).WithMany()
                  .HasForeignKey(e => e.DungeonId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ListId, e.DungeonId }).IsUnique();
        });
    }
}
