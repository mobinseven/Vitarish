namespace Vitarish.Knowledge.Application.Owner;

public sealed record KnowledgeOwnerCorpusOption(string Label, string Details)
{
    internal Guid CorpusId { get; init; }
}

public sealed record KnowledgeOwnerSourceOption(string Label)
{
    internal Guid SourceId { get; init; }
}

public sealed record KnowledgeOwnerImportFormatOption(string Name, string Label);

public sealed record KnowledgeOwnerImportFormats(IReadOnlyList<KnowledgeOwnerImportFormatOption> Options)
{
    public string DefaultName => Options[0].Name;
}

public sealed record KnowledgeOwnerReadModel(IReadOnlyList<KnowledgeOwnerCorpusOption> Corpora, KnowledgeOwnerCorpusOption? SelectedCorpus, IReadOnlyList<KnowledgeOwnerSourceOption> Sources);

public sealed record CreateKnowledgeCorpusInput(string Name, string? Description, bool IsActive);
public sealed record ImportKnowledgeSourceInput(KnowledgeOwnerCorpusOption? Corpus, string? SourceName, string FormatName, string Content);
public sealed record ReplaceKnowledgeSourceInput(KnowledgeOwnerCorpusOption? Corpus, KnowledgeOwnerSourceOption? Source, string? SourceName, string FormatName, string Content);
public sealed record SelectKnowledgeCorpusInput(KnowledgeOwnerCorpusOption? Corpus);
public sealed record IndexKnowledgeCorpusInput(KnowledgeOwnerCorpusOption? Corpus);
public sealed record EvaluateKnowledgeCorpusInput(KnowledgeOwnerCorpusOption? Corpus);

public sealed record KnowledgeOwnerCommandResult(bool Succeeded, string Message)
{
    public static KnowledgeOwnerCommandResult Success(string message) => new(true, message);
    public static KnowledgeOwnerCommandResult Failure(string message) => new(false, message);
}

public sealed record KnowledgeOwnerCreateCorpusResult(bool Succeeded, string Message, KnowledgeOwnerCorpusOption? Corpus)
{
    public static KnowledgeOwnerCreateCorpusResult Success(string message, KnowledgeOwnerCorpusOption corpus) => new(true, message, corpus);
    public static KnowledgeOwnerCreateCorpusResult Failure(string message) => new(false, message, null);
}

public sealed record KnowledgeOwnerImportCommandResult(bool Succeeded, string Message, string? Summary)
{
    public static KnowledgeOwnerImportCommandResult Success(string message, string summary) => new(true, message, summary);
    public static KnowledgeOwnerImportCommandResult Failure(string message) => new(false, message, null);
}

public sealed record KnowledgeOwnerEvaluationCommandResult(bool Succeeded, string Message, string? Summary)
{
    public static KnowledgeOwnerEvaluationCommandResult Success(string message, string summary) => new(true, message, summary);
    public static KnowledgeOwnerEvaluationCommandResult Failure(string message) => new(false, message, null);
}
