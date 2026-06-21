using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Enums;

namespace Vitarish.Knowledge.Infrastructure.Extensions;

public static class KnowledgeOwnerInputValidator
{
    public static string? ValidateTenant(string? tenantId) => string.IsNullOrWhiteSpace(tenantId) ? "Your workspace is unavailable. Please refresh the page and try again." : null;

    public static string? ValidateCreateCorpus(CreateKnowledgeCorpusInput input) => string.IsNullOrWhiteSpace(input.Name) ? "Enter a corpus name." : null;

    public static string? ValidateOwner(string? ownerId) => string.IsNullOrWhiteSpace(ownerId) ? "Your owner identity is unavailable. Please sign in again." : null;

    public static string? ValidateImport(ImportKnowledgeSourceInput input)
    {
        if (input.CorpusId is null) return "Select or create a corpus first.";
        if (string.IsNullOrWhiteSpace(input.Content)) return "Paste content before importing.";
        return null;
    }

    public static string? ValidateFileImport(ImportKnowledgeSourceFromFileInput input)
    {
        if (input.CorpusId is null) return "Select or create a corpus first.";
        if (input.FileInformationId is null) return "Upload a file before importing.";
        return null;
    }

    public static string? ValidateReplace(ReplaceKnowledgeSourceInput input)
    {
        string? importError = ValidateImport(input.ToImportInput());
        if (importError is not null) return importError;
        return input.SourceId is null ? "Choose a source to replace." : null;
    }

    public static string? ValidateFileReplace(ReplaceKnowledgeSourceFromFileInput input)
    {
        if (input.CorpusId is null) return "Select or create a corpus first.";
        if (input.SourceId is null) return "Choose a source to replace.";
        if (input.FileInformationId is null) return "Upload a file before replacing.";
        if (!Enum.IsDefined(typeof(KnowledgeSourceInputFormat), input.Format)) return "Choose a supported content format.";
        return null;
    }

    public static string? ValidateDeleteSource(DeleteKnowledgeSourceInput input)
    {
        if (input.CorpusId is null) return "Select or create a corpus first.";
        if (input.SourceId is null) return "Choose a source to delete.";
        return null;
    }

    public static string? ValidateSelectedCorpus(Guid? corpusId) => corpusId is null ? "Select or create a corpus first." : null;
}
