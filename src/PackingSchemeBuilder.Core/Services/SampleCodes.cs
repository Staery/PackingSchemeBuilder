using System.Text;

namespace PackingSchemeBuilder.Core.Services;

/// <summary>Generates DataMatrix-like unit codes for trying the app.</summary>
public static class SampleCodes
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    /// <summary>Codes of the form 01{GTIN}21{serial}&lt;GS&gt;93{check}, like the ones printed on bottles.</summary>
    public static IEnumerable<string> Generate(string gtin, int count, Random random)
    {
        for (var i = 0; i < count; i++)
        {
            yield return $"01{gtin}21{Random(random, 13)}{Gs1.GroupSeparator}93{Random(random, 4)}";
        }
    }

    private static string Random(Random random, int length)
    {
        var builder = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            builder.Append(Alphabet[random.Next(Alphabet.Length)]);
        }

        return builder.ToString();
    }
}
