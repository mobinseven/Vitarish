namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerImportFormats(IReadOnlyList<KnowledgeOwnerImportFormatOption> Options)
{
    public string DefaultName => Options[0].Name;
}
