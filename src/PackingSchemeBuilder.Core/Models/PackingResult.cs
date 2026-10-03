namespace PackingSchemeBuilder.Core.Models;

public sealed record PackedBottle(int Id, string Code);

public sealed record PackedBox(int Id, string Code, IReadOnlyList<PackedBottle> Bottles);

public sealed record PackedPallet(int Id, string Code, IReadOnlyList<PackedBox> Boxes)
{
    public int BottleCount => Boxes.Sum(box => box.Bottles.Count);
}

/// <summary>Bottles aggregated into boxes and boxes into pallets.</summary>
public sealed record PackingResult(PackagingTask Task, IReadOnlyList<PackedPallet> Pallets)
{
    public static PackingResult Empty(PackagingTask task) => new(task, []);

    public IEnumerable<PackedBox> Boxes => Pallets.SelectMany(pallet => pallet.Boxes);

    public IEnumerable<PackedBottle> Bottles => Boxes.SelectMany(box => box.Bottles);

    public int PalletCount => Pallets.Count;

    public int BoxCount => Pallets.Sum(pallet => pallet.Boxes.Count);

    public int BottleCount => Pallets.Sum(pallet => pallet.BottleCount);

    public int FullBoxCount => Boxes.Count(box => box.Bottles.Count == Task.BoxFormat);

    public int FullPalletCount => Pallets.Count(pallet => pallet.Boxes.Count == Task.PalletFormat && pallet.Boxes.All(box => box.Bottles.Count == Task.BoxFormat));
}
