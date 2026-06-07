using Vitarish.Entities.Server.Infrastructure.Search;

namespace Entities.Server.Tests;

public sealed class EntitySearchTests
{
    [Fact]
    public void Normalize_ReplacesPersianArabicVariantsAndLowercasesLatinText()
    {
        EntitySearchTextNormalizer normalizer = new();

        string normalized = normalizer.Normalize("كِي أ_%\\Z");

        Assert.Equal("کِی ا_%\\z", normalized);
    }

    [Fact]
    public void CreateSearchPatterns_NormalizesEscapesAndSplitsSearchText()
    {
        EntitySearchTextNormalizer normalizer = new();
        EntitySearchPatternFactory factory = new(normalizer);

        IReadOnlyCollection<string> patterns = factory.CreateSearchPatterns(" Foo_%\\ كی ");

        Assert.Equal(
        [
            "%foo\\_\\%\\\\%",
            "%کی%"
        ],
        patterns);
    }
}
