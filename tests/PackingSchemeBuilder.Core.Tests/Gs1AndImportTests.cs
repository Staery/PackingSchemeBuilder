using PackingSchemeBuilder.Core.Services;

namespace PackingSchemeBuilder.Core.Tests;

public class Gs1AndImportTests
{
    private const string Gtin = "04810000123458";

    [Theory]
    [InlineData("04810000123458", true)]
    [InlineData("04810000123459", false)]
    [InlineData("4810000123459", false)]
    [InlineData("0481000012345X", false)]
    [InlineData(null, false)]
    public void IsValidGtin(string? gtin, bool expected) => Assert.Equal(expected, Gs1.IsValidGtin(gtin));

    [Fact]
    public void WithCheckDigit_ProducesValidGtin()
    {
        Assert.Equal("04810000123458", Gs1.WithCheckDigit("0481000012345"));
        Assert.True(Gs1.IsValidGtin(Gs1.WithCheckDigit("0460000000000")));
    }

    [Fact]
    public void AggregationCode_UsesAis01_37_21() =>
        Assert.Equal($"01{Gtin}371221" + "7", Gs1.AggregationCode(Gtin, 12, 7));

    [Fact]
    public void Clean_RemovesGroupSeparatorsAndWhitespace() =>
        Assert.Equal($"01{Gtin}21abc93xyz", Gs1.Clean($" 01{Gtin}21abc\u001D93xyz\r"));

    [Fact]
    public void Import_FiltersProductEmptyLinesAndDuplicates()
    {
        string[] lines =
        [
            $"01{Gtin}21AAA\u001D93aa",
            "",
            $"0104600000000000021BBB\u001D93bb",
            $"01{Gtin}21AAA\u001D93aa",
            $"01{Gtin}21CCC\u001D93cc",
            $"01{Gtin}21KNOWN\u001D93kk",
        ];

        var report = CodeImporter.Import(lines, Gtin, new HashSet<string> { $"01{Gtin}21KNOWN93kk" });

        Assert.Equal([$"01{Gtin}21AAA93aa", $"01{Gtin}21CCC93cc"], report.Accepted);
        Assert.Equal(6, report.Lines);
        Assert.Equal(1, report.Empty);
        Assert.Equal(1, report.OtherProduct);
        Assert.Equal(2, report.Duplicates);
        Assert.Contains("2 new codes", report.Summary);
    }

    [Fact]
    public void Import_DoesNotAcceptGtinInTheMiddleOfAnotherCode()
    {
        // The first version used Contains(gtin), which accepted codes of other products that happened to contain it.
        var report = CodeImporter.Import([$"0104600000000000021{Gtin}"], Gtin);

        Assert.Empty(report.Accepted);
        Assert.Equal(1, report.OtherProduct);
    }

    [Fact]
    public void Parse_SplitsUnitCode()
    {
        var parts = Gs1.Parse($"01{Gtin}21ABCDEFGHIJKLM93xY12");

        Assert.Equal(["01", "21", "93"], parts.Select(p => p.Ai));
        Assert.Equal([Gtin, "ABCDEFGHIJKLM", "xY12"], parts.Select(p => p.Value));
    }

    [Fact]
    public void Parse_SplitsAggregationCode()
    {
        var parts = Gs1.Parse(Gs1.AggregationCode(Gtin, 12, 345));

        Assert.Equal(["01", "37", "21"], parts.Select(p => p.Ai));
        Assert.Equal([Gtin, "12", "345"], parts.Select(p => p.Value));
    }

    [Fact]
    public void Parse_FallsBackToRawCode() => Assert.Equal("Code", Assert.Single(Gs1.Parse("hello")).Name);

    [Fact]
    public void SampleCodes_BelongToProductAndAreUnique()
    {
        var codes = SampleCodes.Generate(Gtin, 500, new Random(1)).ToList();

        Assert.All(codes, code => Assert.True(Gs1.BelongsTo(Gs1.Clean(code), Gtin)));
        Assert.Equal(500, codes.Distinct().Count());
        Assert.All(codes, code => Assert.Contains(Gs1.GroupSeparator, code));
    }
}
