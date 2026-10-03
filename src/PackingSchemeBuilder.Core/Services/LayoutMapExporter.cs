using System.Text.Json;
using System.Text.Json.Serialization;
using PackingSchemeBuilder.Core.Models;

namespace PackingSchemeBuilder.Core.Services;

/// <summary>Writes the packing layout as JSON: one entry per pallet with its boxes and their bottles.</summary>
public static class LayoutMapExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string ToJson(PackingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var maps = result.Pallets.Select(pallet => new LayoutMap(
            result.Task.ProductName,
            result.Task.Gtin,
            result.Task.BoxFormat,
            result.Task.PalletFormat,
            new PalletMap(pallet.Id, pallet.Code, pallet.Boxes.Select(box =>
                new BoxMap(box.Id, box.Code, box.Bottles.Select(bottle => new BottleMap(bottle.Id, bottle.Code)).ToList())).ToList())));

        return JsonSerializer.Serialize(maps, Options);
    }

    public static string SuggestFileName(PackagingTask task, DateTimeOffset now) => $"{task.Gtin}_layout_{now:yyyyMMdd_HHmm}.json";

    public static Task ExportAsync(PackingResult result, string path, CancellationToken cancellationToken = default) =>
        File.WriteAllTextAsync(path, ToJson(result), cancellationToken);

    private sealed record LayoutMap(string ProductName, string Gtin, int BoxFormat, int PalletFormat, PalletMap Pallet);

    private sealed record PalletMap(int Id, string Code, IReadOnlyList<BoxMap> Boxes);

    private sealed record BoxMap(int Id, string Code, IReadOnlyList<BottleMap> Bottles);

    private sealed record BottleMap(int Id, string Code);
}
