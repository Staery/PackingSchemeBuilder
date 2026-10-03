using PackingSchemeBuilder.Core.Services;

namespace PackingSchemeBuilder.Core.Models;

/// <summary>A packaging job: which product is packed and how many units go into a box and boxes onto a pallet.</summary>
/// <param name="Volume">Bottle volume as reported by the marking system, e.g. "0.5".</param>
/// <param name="BoxFormat">Bottles per box.</param>
/// <param name="PalletFormat">Boxes per pallet.</param>
public sealed record PackagingTask(string ProductName, string Gtin, string Volume, int BoxFormat, int PalletFormat)
{
    public int BottlesPerPallet => BoxFormat * PalletFormat;

    /// <summary>A sample task for trying the app without access to the marking server.</summary>
    public static PackagingTask Demo { get; } = new("Still mineral water “Staery Spring”", Gs1.WithCheckDigit("0481000012345"), "0.5", 12, 8);

    /// <summary>Returns an error message, or <see langword="null"/> if the task can be used for packing.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(ProductName))
        {
            return "The task has no product name.";
        }

        if (!Gs1.IsValidGtin(Gtin))
        {
            return $"“{Gtin}” is not a valid 14-digit GTIN.";
        }

        if (BoxFormat < 1 || PalletFormat < 1)
        {
            return "Box and pallet formats must be at least 1.";
        }

        return null;
    }
}
