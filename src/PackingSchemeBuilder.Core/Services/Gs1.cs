using System.Text.RegularExpressions;

namespace PackingSchemeBuilder.Core.Services;

/// <summary>One GS1 element of a code: application identifier, its meaning and the value.</summary>
public sealed record CodePart(string Ai, string Name, string Value);

/// <summary>GS1 helpers: GTIN validation and aggregation codes for boxes and pallets.</summary>
public static partial class Gs1
{
    /// <summary>Group separator (FNC1) used inside DataMatrix codes.</summary>
    public const char GroupSeparator = '\u001D';

    [GeneratedRegex(@"[\x00-\x1F\x7F]")]
    private static partial Regex ControlCharacters();

    /// <summary>Whether <paramref name="gtin"/> is 14 digits with a correct check digit.</summary>
    public static bool IsValidGtin(string? gtin) =>
        gtin is { Length: 14 } && gtin.All(char.IsAsciiDigit) && gtin[13] - '0' == CheckDigit(gtin.AsSpan(0, 13));

    /// <summary>Appends the GS1 mod-10 check digit to 13 digits.</summary>
    public static string WithCheckDigit(string thirteenDigits)
    {
        if (thirteenDigits.Length != 13 || !thirteenDigits.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Exactly 13 digits are required.", nameof(thirteenDigits));
        }

        return thirteenDigits + CheckDigit(thirteenDigits);
    }

    /// <summary>
    /// Aggregation code of a box or pallet: AI (01) GTIN, AI (37) number of contained units, AI (21) serial number.
    /// The format matches the one expected by the marking system.
    /// </summary>
    public static string AggregationCode(string gtin, int count, int serial) => $"01{gtin}37{count}21{serial}";

    /// <summary>Removes control characters such as the group separator that scanners put into DataMatrix codes.</summary>
    public static string Clean(string code) => ControlCharacters().Replace(code, string.Empty).Trim();

    /// <summary>Whether a unit code starts with AI (01) followed by <paramref name="gtin"/>.</summary>
    public static bool BelongsTo(string code, string gtin) => code.StartsWith("01" + gtin, StringComparison.Ordinal);

    /// <summary>
    /// Splits a cleaned unit code of the form (01) GTIN (21) 13-character serial (93) check code into its elements.
    /// Anything that does not follow this layout is returned as a single "raw" part.
    /// </summary>
    public static IReadOnlyList<CodePart> Parse(string code)
    {
        if (code.Length >= 33 && code.StartsWith("01", StringComparison.Ordinal) && code.AsSpan(2, 14).ToString().All(char.IsAsciiDigit) && code.Substring(16, 2) == "21")
        {
            var parts = new List<CodePart>
            {
                new("01", "GTIN", code.Substring(2, 14)),
                new("21", "Serial number", code.Substring(18, 13)),
            };

            var rest = code[31..];
            if (rest.StartsWith("93", StringComparison.Ordinal))
            {
                parts.Add(new CodePart("93", "Check code", rest[2..]));
            }
            else if (rest.Length > 0)
            {
                parts.Add(new CodePart("", "Remainder", rest));
            }

            return parts;
        }

        if (code.StartsWith("01", StringComparison.Ordinal) && code.Length > 18 && code.IndexOf("37", 16, StringComparison.Ordinal) == 16)
        {
            var serialAt = code.IndexOf("21", 18, StringComparison.Ordinal);
            if (serialAt > 18)
            {
                return
                [
                    new("01", "GTIN", code.Substring(2, 14)),
                    new("37", "Units inside", code[18..serialAt]),
                    new("21", "Serial number", code[(serialAt + 2)..]),
                ];
            }
        }

        return [new CodePart("", "Code", code)];
    }

    private static int CheckDigit(ReadOnlySpan<char> digits)
    {
        // Weights 3,1,3,1... from the rightmost digit.
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var weight = (digits.Length - i) % 2 == 1 ? 3 : 1;
            sum += (digits[i] - '0') * weight;
        }

        return (10 - sum % 10) % 10;
    }
}
