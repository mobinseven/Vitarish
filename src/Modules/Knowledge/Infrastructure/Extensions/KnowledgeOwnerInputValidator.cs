using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Owner;

public static class KnowledgeOwnerInputValidator
{
    public static string? ValidateTenant(string? tenantId) => string.IsNullOrWhiteSpace(tenantId) ? "Your workspace is unavailable. Please refresh the page and try again." : null;

    public static string? ValidateCreateCorpus(CreateKnowledgeCorpusInput input) => string.IsNullOrWhiteSpace(input.Name) ? "Enter a corpus name." : null;

    public static string? ValidateImport(ImportKnowledgeSourceInput input)
    {
        if (input.CorpusId is null) return "Select or create a corpus first.";
        if (string.IsNullOrWhiteSpace(input.FormatName) || !KnowledgeOwnerImportFormatCatalog.Contains(input.FormatName)) return "Choose a supported content format.";
        if (string.IsNullOrWhiteSpace(input.Content)) return "Paste content before importing.";
        return null;
    }

    public static string? ValidateFileImport(ImportKnowledgeSourceFromFileInput input)
    {
        if (input.CorpusId is null) return "Select or create a corpus first.";
        if (input.FileInformationId is null) return "Upload a file before importing.";
        if (string.IsNullOrWhiteSpace(input.FormatName) || !KnowledgeOwnerImportFormatCatalog.Contains(input.FormatName)) return "Choose a supported content format.";
        return null;
    }

    public static string? ValidateReplace(ReplaceKnowledgeSourceInput input)
    {
        string? importError = ValidateImport(input.ToImportInput());
        if (importError is not null) return importError;
        return input.SourceId is null ? "Choose a source to replace." : null;
    }

    public static string? ValidateSelectedCorpus(Guid? corpusId) => corpusId is null ? "Select or create a corpus first." : null;
}
