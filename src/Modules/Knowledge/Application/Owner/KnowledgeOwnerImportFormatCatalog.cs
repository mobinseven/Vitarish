namespace Vitarish.Knowledge.Application.Owner;

public static class KnowledgeOwnerImportFormatCatalog
{
    private static readonly KnowledgeOwnerImportFormatOption[] SupportedFormats =
    [
        new("markdown", "Markdown"),
        new("plain-text", "Plain text"),
        new("techqa-json", "TechQA JSON"),
        new("rag-eval-json", "RAG evaluation JSON")
    ];

    public static KnowledgeOwnerImportFormats Current { get; } = new(SupportedFormats);
    public static bool Contains(string formatName) => SupportedFormats.Any(format => string.Equals(format.Name, formatName, StringComparison.Ordinal));
}
