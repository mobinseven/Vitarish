namespace Vitarish.Knowledge.Application.Owner;

public static class KnowledgeOwnerInputValidator
{
    public static string? ValidateCreateCorpus(CreateKnowledgeCorpusInput input) => string.IsNullOrWhiteSpace(input.Name) ? "Enter a corpus name." : null;

    public static string? ValidateImport(ImportKnowledgeSourceInput input)
    {
        if (input.Corpus is null) return "Select or create a corpus first.";
        if (string.IsNullOrWhiteSpace(input.FormatName) || !KnowledgeOwnerImportFormatCatalog.Contains(input.FormatName)) return "Choose a supported content format.";
        if (string.IsNullOrWhiteSpace(input.Content)) return "Paste content before importing.";
        return null;
    }

    public static string? ValidateReplace(ReplaceKnowledgeSourceInput input)
    {
        string? importError = ValidateImport(new ImportKnowledgeSourceInput(input.Corpus, input.SourceName, input.FormatName, input.Content));
        if (importError is not null) return importError;
        return input.Source is null ? "Choose a source to replace." : null;
    }

    public static string? ValidateSelectedCorpus(KnowledgeOwnerCorpusOption? corpus) => corpus is null ? "Select or create a corpus first." : null;
}
