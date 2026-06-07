using System.Text;

namespace Vitarish.Entities.Server.Infrastructure.Search;

public sealed class EntitySearchTextNormalizer
{
    private static readonly IReadOnlyList<(string Variant, string Replacement)> NormalizationPairs =
    [
        ("\u200c", string.Empty), // Zero-width non-joiner
        ("ك", "ک"),
        ("ي", "ی"),
        ("ى", "ی"),
        ("ئ", "ی"),
        ("ة", "ه"),
        ("ۀ", "ه"),
        ("أ", "ا"),
        ("إ", "ا"),
        ("آ", "ا"),
        ("ؤ", "و")
    ];

    private static readonly IReadOnlyDictionary<char, string> NormalizationLookup =
        NormalizationPairs
            .Where(pair => pair.Variant.Length == 1)
            .ToDictionary(pair => pair.Variant[0], pair => pair.Replacement);

    public IReadOnlyList<(string Variant, string Replacement)> Replacements => NormalizationPairs;

    public string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            return string.Empty;
        }

        string normalized = value.Normalize(NormalizationForm.FormKC);
        StringBuilder builder = new(normalized.Length);

        foreach (char character in normalized)
        {
            _ = NormalizationLookup.TryGetValue(character, out string? replacement)
                ? builder.Append(replacement)
                : builder.Append(character);
        }

        return builder.ToString().ToLowerInvariant();
    }
}
