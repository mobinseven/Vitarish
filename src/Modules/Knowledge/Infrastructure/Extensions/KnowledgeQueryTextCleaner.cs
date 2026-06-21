using System.Text.RegularExpressions;

namespace Vitarish.Knowledge.Infrastructure.Services.RetrievalPlanning;

public static partial class KnowledgeQueryTextCleaner
{
    public static string Clean(string question)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        string compactWhitespace = WhitespacePattern().Replace(question.Trim(), " ");
        string compactPunctuation = RepeatedPunctuationPattern().Replace(compactWhitespace, "$1");
        return compactPunctuation;
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex("([?!.,;:])\\1+")]
    private static partial Regex RepeatedPunctuationPattern();
}
