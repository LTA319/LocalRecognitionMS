using Microsoft.EntityFrameworkCore;
using ObjectRecognitionSystem.Models;

namespace ObjectRecognitionSystem.Data;

public class AppDbContext : DbContext
{
    public DbSet<ItemInfo> Items { get; set; } = null!;
    public DbSet<NameMapping> NameMappings { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite(AppConfig.ConnectionString);

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<ItemInfo>(e =>
        {
            e.ToTable("Items");
            e.HasIndex(i => i.Name);
            e.HasIndex(i => i.Code);
        });

        model.Entity<NameMapping>(e =>
        {
            e.ToTable("NameMappings");
            e.HasIndex(m => m.DetectedName);
        });
    }

    public void EnsureFts5Created()
    {
        Database.ExecuteSqlRaw(@"
            CREATE VIRTUAL TABLE IF NOT EXISTS ItemsFTS USING fts5(
                Name, Code, Specification, DisplayName,
                content='Items', content_rowid='Id'
            );

            CREATE TRIGGER IF NOT EXISTS items_ai AFTER INSERT ON Items BEGIN
                INSERT INTO ItemsFTS(rowid, Name, Code, Specification, DisplayName)
                VALUES (new.Id, new.Name, new.Code, new.Specification, new.DisplayName);
            END;

            CREATE TRIGGER IF NOT EXISTS items_ad AFTER DELETE ON Items BEGIN
                INSERT INTO ItemsFTS(ItemsFTS, rowid, Name, Code, Specification, DisplayName)
                VALUES ('delete', old.Id, old.Name, old.Code, old.Specification, old.DisplayName);
            END;

            CREATE TRIGGER IF NOT EXISTS items_au AFTER UPDATE ON Items BEGIN
                INSERT INTO ItemsFTS(ItemsFTS, rowid, Name, Code, Specification, DisplayName)
                VALUES ('delete', old.Id, old.Name, old.Code, old.Specification, old.DisplayName);
                INSERT INTO ItemsFTS(rowid, Name, Code, Specification, DisplayName)
                VALUES (new.Id, new.Name, new.Code, new.Specification, new.DisplayName);
            END;
        ");
    }
}
