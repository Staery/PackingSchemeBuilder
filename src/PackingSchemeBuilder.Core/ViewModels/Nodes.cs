using PackingSchemeBuilder.Core.Models;
using PackingSchemeBuilder.Core.Services;

namespace PackingSchemeBuilder.Core.ViewModels;

/// <summary>A node of the pallet → box → bottle tree.</summary>
public abstract class PackingNode
{
    public abstract string Kind { get; }

    public abstract string Title { get; }

    public abstract string Code { get; }

    /// <summary>Short description of the contents, e.g. "8 of 8 boxes".</summary>
    public abstract string Details { get; }

    public abstract bool IsComplete { get; }

    public abstract IReadOnlyList<PackingNode> Children { get; }

    /// <summary>The code split into its GS1 elements, for the details panel.</summary>
    public IReadOnlyList<CodePart> CodeParts => Gs1.Parse(Code);

    /// <summary>Short label used for the children overview, e.g. "12" for a box with 12 bottles.</summary>
    public virtual string Badge => Children.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class PalletNode(PackedPallet pallet, PackagingTask task) : PackingNode
{
    private IReadOnlyList<PackingNode>? _children;

    public PackedPallet Pallet { get; } = pallet;

    public override string Kind => "Pallet";

    public override string Title => $"Pallet {Pallet.Id}";

    public override string Code => Pallet.Code;

    public override string Details => $"{Pallet.Boxes.Count} of {task.PalletFormat} boxes · {Pallet.BottleCount} bottles";

    public override bool IsComplete => Pallet.Boxes.Count == task.PalletFormat && Pallet.BottleCount == task.BottlesPerPallet;

    public override IReadOnlyList<PackingNode> Children => _children ??= Pallet.Boxes.Select(box => new BoxNode(box, task)).ToList();
}

public sealed class BoxNode(PackedBox box, PackagingTask task) : PackingNode
{
    private IReadOnlyList<PackingNode>? _children;

    public PackedBox Box { get; } = box;

    public override string Kind => "Box";

    public override string Title => $"Box {Box.Id}";

    public override string Code => Box.Code;

    public override string Details => $"{Box.Bottles.Count} of {task.BoxFormat} bottles";

    public override bool IsComplete => Box.Bottles.Count == task.BoxFormat;

    public override IReadOnlyList<PackingNode> Children => _children ??= Box.Bottles.Select(bottle => new BottleNode(bottle)).ToList();
}

public sealed class BottleNode(PackedBottle bottle) : PackingNode
{
    public PackedBottle Bottle { get; } = bottle;

    public override string Kind => "Bottle";

    public override string Title => $"Bottle {Bottle.Id}";

    public override string Code => Bottle.Code;

    public override string Details => "Unit code";

    public override bool IsComplete => true;

    public override IReadOnlyList<PackingNode> Children => [];

    public override string Badge => Bottle.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
