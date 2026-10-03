using System.Text.Json;
using PackingSchemeBuilder.Core.Models;
using PackingSchemeBuilder.Core.Services;

namespace PackingSchemeBuilder.Core.Tests;

public class PlannerAndExportTests
{
    private static readonly PackagingTask Task = new("Water", "04810000123458", "0.5", BoxFormat: 3, PalletFormat: 2);

    private static List<string> Codes(int count) => Enumerable.Range(1, count).Select(i => $"0104810000123458210{i:000}").ToList();

    [Fact]
    public void Plan_FillsBoxesAndPalletsInOrder()
    {
        var result = PackingPlanner.Plan(Task, Codes(14));

        Assert.Equal(3, result.PalletCount);
        Assert.Equal(5, result.BoxCount);
        Assert.Equal(14, result.BottleCount);
        Assert.Equal(4, result.FullBoxCount);
        Assert.Equal(2, result.FullPalletCount);

        Assert.Equal([2, 2, 1], result.Pallets.Select(p => p.Boxes.Count));
        Assert.Equal([3, 3, 3, 3, 2], result.Boxes.Select(b => b.Bottles.Count));
        Assert.Equal(Enumerable.Range(1, 14), result.Bottles.Select(b => b.Id));
        Assert.Equal(Codes(14), result.Bottles.Select(b => b.Code));
    }

    [Fact]
    public void Plan_GeneratesAggregationCodesWithContentCount()
    {
        var result = PackingPlanner.Plan(Task, Codes(14));

        Assert.Equal("010481000012345837321" + "1", result.Boxes.First().Code);
        Assert.Equal("010481000012345837221" + "5", result.Boxes.Last().Code);
        Assert.Equal("010481000012345837121" + "3", result.Pallets[2].Code);
        Assert.Equal(result.BoxCount + result.PalletCount, result.Boxes.Select(b => b.Code).Concat(result.Pallets.Select(p => p.Code)).Distinct().Count());
    }

    [Fact]
    public void Plan_ExactMultipleHasNoPartialContainers()
    {
        var result = PackingPlanner.Plan(Task, Codes(12));

        Assert.Equal(2, result.PalletCount);
        Assert.Equal(result.BoxCount, result.FullBoxCount);
        Assert.Equal(2, result.FullPalletCount);
    }

    [Fact]
    public void Plan_EmptyAndInvalid()
    {
        Assert.Equal(0, PackingPlanner.Plan(Task, []).BottleCount);
        Assert.Throws<ArgumentException>(() => PackingPlanner.Plan(Task with { BoxFormat = 0 }, Codes(1)));
        Assert.Throws<ArgumentException>(() => PackingPlanner.Plan(Task with { Gtin = "123" }, Codes(1)));
    }

    [Fact]
    public void Export_ProducesOneLayoutMapPerPallet()
    {
        var json = LayoutMapExporter.ToJson(PackingPlanner.Plan(Task, Codes(7)));

        using var document = JsonDocument.Parse(json);
        var maps = document.RootElement;
        Assert.Equal(2, maps.GetArrayLength());

        var first = maps[0];
        Assert.Equal("Water", first.GetProperty("productName").GetString());
        Assert.Equal("04810000123458", first.GetProperty("gtin").GetString());
        Assert.Equal(3, first.GetProperty("boxFormat").GetInt32());
        Assert.Equal(2, first.GetProperty("palletFormat").GetInt32());

        var pallet = first.GetProperty("pallet");
        Assert.Equal(1, pallet.GetProperty("id").GetInt32());
        Assert.Equal(2, pallet.GetProperty("boxes").GetArrayLength());
        Assert.Equal(3, pallet.GetProperty("boxes")[0].GetProperty("bottles").GetArrayLength());
        Assert.Equal(Codes(1)[0], pallet.GetProperty("boxes")[0].GetProperty("bottles")[0].GetProperty("code").GetString());
    }

    [Fact]
    public void Export_FileNameIsSortableAndZeroPadded() =>
        Assert.Equal("04810000123458_layout_20260304_0705.json", LayoutMapExporter.SuggestFileName(Task, new DateTimeOffset(2026, 3, 4, 7, 5, 0, TimeSpan.Zero)));
}
