using Microsoft.EntityFrameworkCore;

namespace PackingSchemeBuilder.Core.Data;

/// <summary>SQLite database with the current task and its pallets, boxes and bottles.</summary>
public sealed class PackingDbContext(DbContextOptions<PackingDbContext> options) : DbContext(options)
{
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    public DbSet<PalletEntity> Pallets => Set<PalletEntity>();

    public DbSet<BoxEntity> Boxes => Set<BoxEntity>();

    public DbSet<BottleEntity> Bottles => Set<BottleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskEntity>().ToTable("Task");

        modelBuilder.Entity<PalletEntity>(pallet =>
        {
            pallet.ToTable("Pallet");
            pallet.Property(p => p.Id).ValueGeneratedNever();
            pallet.HasIndex(p => p.Code).IsUnique();
        });

        modelBuilder.Entity<BoxEntity>(box =>
        {
            box.ToTable("Box");
            box.Property(b => b.Id).ValueGeneratedNever();
            box.HasIndex(b => b.Code).IsUnique();
            box.HasOne(b => b.Pallet).WithMany(p => p.Boxes).HasForeignKey(b => b.PalletId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BottleEntity>(bottle =>
        {
            bottle.ToTable("Bottle");
            bottle.Property(b => b.Id).ValueGeneratedNever();
            bottle.HasIndex(b => b.Code).IsUnique();
            bottle.HasOne(b => b.Box).WithMany(b => b.Bottles).HasForeignKey(b => b.BoxId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public sealed class TaskEntity
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Gtin { get; set; } = string.Empty;

    public string Volume { get; set; } = string.Empty;

    public int BoxFormat { get; set; }

    public int PalletFormat { get; set; }
}

public sealed class PalletEntity
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public List<BoxEntity> Boxes { get; set; } = [];
}

public sealed class BoxEntity
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public int PalletId { get; set; }

    public PalletEntity? Pallet { get; set; }

    public List<BottleEntity> Bottles { get; set; } = [];
}

public sealed class BottleEntity
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public int BoxId { get; set; }

    public BoxEntity? Box { get; set; }
}
