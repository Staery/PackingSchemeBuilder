using Microsoft.EntityFrameworkCore;
using PackingSchemeBuilder.Core.Models;

namespace PackingSchemeBuilder.Core.Data;

/// <summary>Stores the current packing result.</summary>
public interface IPackingRepository
{
    string Location { get; }

    Task<PackingResult?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces everything that is stored with <paramref name="result"/>, in one transaction.</summary>
    Task SaveAsync(PackingResult result, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>EF Core + SQLite implementation of <see cref="IPackingRepository"/>.</summary>
public sealed class PackingRepository(Func<PackingDbContext> createContext, string location) : IPackingRepository
{
    public string Location { get; } = location;

    public static PackingRepository CreateDefault()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PackingSchemeBuilder");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "packing.db");
        return ForFile(path);
    }

    public static PackingRepository ForFile(string path)
    {
        var options = new DbContextOptionsBuilder<PackingDbContext>().UseSqlite($"Data Source={path}").Options;
        return new PackingRepository(() => new PackingDbContext(options), path);
    }

    public async Task<PackingResult?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await OpenAsync(cancellationToken).ConfigureAwait(false);

        var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            return null;
        }

        var pallets = await db.Pallets.AsNoTracking()
            .Include(p => p.Boxes).ThenInclude(b => b.Bottles)
            .OrderBy(p => p.Id)
            .AsSplitQuery()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PackingResult(
            new PackagingTask(task.ProductName, task.Gtin, task.Volume, task.BoxFormat, task.PalletFormat),
            pallets.Select(p => new PackedPallet(p.Id, p.Code, p.Boxes.OrderBy(b => b.Id).Select(b =>
                new PackedBox(b.Id, b.Code, b.Bottles.OrderBy(x => x.Id).Select(x => new PackedBottle(x.Id, x.Code)).ToList())).ToList())).ToList());
    }

    public async Task SaveAsync(PackingResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        await using var db = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await ClearTablesAsync(db, cancellationToken).ConfigureAwait(false);

        db.Tasks.Add(new TaskEntity
        {
            Id = 1,
            ProductName = result.Task.ProductName,
            Gtin = result.Task.Gtin,
            Volume = result.Task.Volume,
            BoxFormat = result.Task.BoxFormat,
            PalletFormat = result.Task.PalletFormat,
        });

        db.Pallets.AddRange(result.Pallets.Select(p => new PalletEntity
        {
            Id = p.Id,
            Code = p.Code,
            Boxes = p.Boxes.Select(b => new BoxEntity
            {
                Id = b.Id,
                Code = b.Code,
                Bottles = b.Bottles.Select(x => new BottleEntity { Id = x.Id, Code = x.Code }).ToList(),
            }).ToList(),
        }));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await ClearTablesAsync(db, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ClearTablesAsync(PackingDbContext db, CancellationToken cancellationToken)
    {
        await db.Bottles.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await db.Boxes.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await db.Pallets.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await db.Tasks.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<PackingDbContext> OpenAsync(CancellationToken cancellationToken)
    {
        var db = createContext();
        await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        return db;
    }
}
