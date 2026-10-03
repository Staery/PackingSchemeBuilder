namespace PackingSchemeBuilder.Core.Services;

/// <summary>Outcome of reading a file of unit codes.</summary>
public sealed record ImportReport(IReadOnlyList<string> Accepted, int Lines, int Empty, int OtherProduct, int Duplicates)
{
    public string Summary =>
        $"{Accepted.Count:N0} new codes accepted from {Lines:N0} lines" +
        (OtherProduct > 0 ? $" · {OtherProduct:N0} for another product" : string.Empty) +
        (Duplicates > 0 ? $" · {Duplicates:N0} duplicates skipped" : string.Empty) +
        (Empty > 0 ? $" · {Empty:N0} empty" : string.Empty);
}

/// <summary>Cleans unit codes, keeps those of the task's product and drops duplicates.</summary>
public static class CodeImporter
{
    /// <param name="known">Codes that are already packed; they count as duplicates.</param>
    public static ImportReport Import(IEnumerable<string> lines, string gtin, IReadOnlySet<string>? known = null)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var accepted = new List<string>();
        var seen = new HashSet<string>(known ?? new HashSet<string>(), StringComparer.Ordinal);
        int total = 0, empty = 0, otherProduct = 0, duplicates = 0;

        foreach (var line in lines)
        {
            total++;
            var code = Gs1.Clean(line);

            if (code.Length == 0)
            {
                empty++;
            }
            else if (!Gs1.BelongsTo(code, gtin))
            {
                otherProduct++;
            }
            else if (!seen.Add(code))
            {
                duplicates++;
            }
            else
            {
                accepted.Add(code);
            }
        }

        return new ImportReport(accepted, total, empty, otherProduct, duplicates);
    }
}
