using PackingSchemeBuilder.Core.Models;

namespace PackingSchemeBuilder.Core.Services;

/// <summary>
/// Aggregates unit codes into boxes and boxes into pallets, in the order the codes were scanned.
/// Only the last box and the last pallet can be incomplete.
/// </summary>
public static class PackingPlanner
{
    public static PackingResult Plan(PackagingTask task, IReadOnlyList<string> codes)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(codes);

        if (task.Validate() is { } error)
        {
            throw new ArgumentException(error, nameof(task));
        }

        var pallets = new List<PackedPallet>();
        var bottleId = 0;
        var boxId = 0;

        foreach (var palletCodes in codes.Chunk(task.BottlesPerPallet))
        {
            var boxes = new List<PackedBox>();

            foreach (var boxCodes in palletCodes.Chunk(task.BoxFormat))
            {
                boxId++;
                var bottles = boxCodes.Select(code => new PackedBottle(++bottleId, code)).ToList();
                boxes.Add(new PackedBox(boxId, Gs1.AggregationCode(task.Gtin, bottles.Count, boxId), bottles));
            }

            var palletId = pallets.Count + 1;
            pallets.Add(new PackedPallet(palletId, Gs1.AggregationCode(task.Gtin, boxes.Count, palletId), boxes));
        }

        return new PackingResult(task, pallets);
    }
}
