using System.Globalization;
using System.Text;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Owner;

public interface IKnowledgeOwnerUseCases
{
    KnowledgeOwnerImportFormats ImportFormats { get; }
    Task<KnowledgeOwnerReadModel> LoadAsync(string? tenantId, KnowledgeOwnerCorpusOption? selectedCorpus, CancellationToken cancellationToken);
    Task<KnowledgeOwnerCommandResult> CreateCorpusAsync(string? tenantId, CreateKnowledgeCorpusInput input, CancellationToken cancellationToken);
    Task<KnowledgeOwnerReadModel> SelectCorpusAsync(string? tenantId, SelectKnowledgeCorpusInput input, CancellationToken cancellationToken);
    Task<KnowledgeOwnerImportCommandResult> ImportSourceAsync(string? tenantId, ImportKnowledgeSourceInput input, CancellationToken cancellationToken);
    Task<KnowledgeOwnerImportCommandResult> ReplaceSourceAsync(string? tenantId, ReplaceKnowledgeSourceInput input, CancellationToken cancellationToken);
    Task<KnowledgeOwnerCommandResult> IndexCorpusAsync(string? tenantId, IndexKnowledgeCorpusInput input, CancellationToken cancellationToken);
    Task<KnowledgeOwnerEvaluationCommandResult> EvaluateCorpusAsync(string? tenantId, EvaluateKnowledgeCorpusInput input, CancellationToken cancellationToken);
}

public sealed class KnowledgeOwnerUseCases(IKnowledgeService knowledge) : IKnowledgeOwnerUseCases
{
    public KnowledgeOwnerImportFormats ImportFormats => KnowledgeOwnerImportFormatCatalog.Current;

    public async Task<KnowledgeOwnerReadModel> LoadAsync(string? tenantId, KnowledgeOwnerCorpusOption? selectedCorpus, CancellationToken cancellationToken)
    {
        string? tenantError = ValidateTenant(tenantId);
        if (tenantError is not null) return new KnowledgeOwnerReadModel([], null, []);
        IReadOnlyList<KnowledgeCorpus> corpora = await knowledge.ListCorporaAsync(tenantId!, cancellationToken);
        KnowledgeOwnerCorpusOption? selected = BuildCorpusOptions(corpora).FirstOrDefault(option => selectedCorpus is not null && option.CorpusId == selectedCorpus.CorpusId) ?? BuildCorpusOptions(corpora).FirstOrDefault();
        return await BuildReadModelAsync(tenantId!, corpora, selected, cancellationToken);
    }

    public async Task<KnowledgeOwnerCommandResult> CreateCorpusAsync(string? tenantId, CreateKnowledgeCorpusInput input, CancellationToken cancellationToken)
    {
        string? error = ValidateTenant(tenantId) ?? KnowledgeOwnerInputValidator.ValidateCreateCorpus(input);
        if (error is not null) return KnowledgeOwnerCommandResult.Failure(error);
        KnowledgeCorpus corpus = new() { Id = Guid.NewGuid(), TenantId = tenantId!, Name = input.Name.Trim(), Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(), IsActive = input.IsActive };
        await knowledge.SaveCorpusAsync(corpus, cancellationToken);
        return KnowledgeOwnerCommandResult.Success("Corpus created.");
    }

    public async Task<KnowledgeOwnerReadModel> SelectCorpusAsync(string? tenantId, SelectKnowledgeCorpusInput input, CancellationToken cancellationToken) => await LoadAsync(tenantId, input.Corpus, cancellationToken);

    public async Task<KnowledgeOwnerImportCommandResult> ImportSourceAsync(string? tenantId, ImportKnowledgeSourceInput input, CancellationToken cancellationToken)
    {
        string? error = ValidateTenant(tenantId) ?? KnowledgeOwnerInputValidator.ValidateImport(input);
        if (error is not null) return KnowledgeOwnerImportCommandResult.Failure(error);
        await using MemoryStream content = new(Encoding.UTF8.GetBytes(input.Content));
        KnowledgeImportResult result = await knowledge.ImportAsync(BuildImportRequest(tenantId!, input.Corpus!, input, content), cancellationToken);
        return KnowledgeOwnerImportCommandResult.Success("Import complete.", $"Imported {result.Sources.Count} source(s), {result.Documents.Count} document(s), and {result.Chunks.Count} searchable section(s).");
    }

    public async Task<KnowledgeOwnerImportCommandResult> ReplaceSourceAsync(string? tenantId, ReplaceKnowledgeSourceInput input, CancellationToken cancellationToken)
    {
        string? error = ValidateTenant(tenantId) ?? KnowledgeOwnerInputValidator.ValidateReplace(input);
        if (error is not null) return KnowledgeOwnerImportCommandResult.Failure(error);
        await using MemoryStream content = new(Encoding.UTF8.GetBytes(input.Content));
        KnowledgeImportRequest request = BuildImportRequest(tenantId!, input.Corpus!, new ImportKnowledgeSourceInput(input.Corpus, input.SourceName, input.FormatName, input.Content), content);
        KnowledgeImportResult result = await knowledge.ReplaceSourceAsync(input.Source!.SourceId, request, cancellationToken);
        return KnowledgeOwnerImportCommandResult.Success("Source replacement complete.", $"Imported {result.Sources.Count} source(s), {result.Documents.Count} document(s), and {result.Chunks.Count} searchable section(s).");
    }

    public async Task<KnowledgeOwnerCommandResult> IndexCorpusAsync(string? tenantId, IndexKnowledgeCorpusInput input, CancellationToken cancellationToken)
    {
        string? error = ValidateTenant(tenantId) ?? KnowledgeOwnerInputValidator.ValidateSelectedCorpus(input.Corpus);
        if (error is not null) return KnowledgeOwnerCommandResult.Failure(error);
        KnowledgeIndexResult result = await knowledge.IndexAsync(input.Corpus!.CorpusId, tenantId!, cancellationToken);
        return KnowledgeOwnerCommandResult.Success($"Indexed {result.IndexedChunkCount} searchable section(s).");
    }

    public async Task<KnowledgeOwnerEvaluationCommandResult> EvaluateCorpusAsync(string? tenantId, EvaluateKnowledgeCorpusInput input, CancellationToken cancellationToken)
    {
        string? error = ValidateTenant(tenantId) ?? KnowledgeOwnerInputValidator.ValidateSelectedCorpus(input.Corpus);
        if (error is not null) return KnowledgeOwnerEvaluationCommandResult.Failure(error);
        KnowledgeEvaluationRun run = await knowledge.EvaluateAsync(new KnowledgeEvaluationRequest(input.Corpus!.CorpusId, tenantId!), cancellationToken);
        string summary = string.Create(CultureInfo.InvariantCulture, $"recall@5 {run.RecallAt5:0.###} · recall@10 {run.RecallAt10:0.###} · average expected rank {run.AverageExpectedChunkRank:0.###}");
        return KnowledgeOwnerEvaluationCommandResult.Success("Evaluation complete.", summary);
    }

    private static KnowledgeImportRequest BuildImportRequest(string tenantId, KnowledgeOwnerCorpusOption corpus, ImportKnowledgeSourceInput input, Stream content) => new() { CorpusId = corpus.CorpusId, TenantId = tenantId, Content = content, Format = input.FormatName.Trim(), SourceName = string.IsNullOrWhiteSpace(input.SourceName) ? null : input.SourceName.Trim() };
    private async Task<KnowledgeOwnerReadModel> BuildReadModelAsync(string tenantId, IReadOnlyList<KnowledgeCorpus> corpora, KnowledgeOwnerCorpusOption? selected, CancellationToken cancellationToken)
    {
        IReadOnlyList<KnowledgeOwnerSourceOption> sources = selected is null ? [] : (await knowledge.GetSourcesAsync(selected.CorpusId, tenantId, cancellationToken)).Select(ToSourceOption).ToArray();
        return new KnowledgeOwnerReadModel(BuildCorpusOptions(corpora), selected, sources);
    }

    private static IReadOnlyList<KnowledgeOwnerCorpusOption> BuildCorpusOptions(IReadOnlyList<KnowledgeCorpus> corpora) => corpora.Select(ToCorpusOption).ToArray();
    private static KnowledgeOwnerCorpusOption ToCorpusOption(KnowledgeCorpus corpus) => new(string.IsNullOrWhiteSpace(corpus.Description) ? corpus.Name : $"{corpus.Name} — {corpus.Description}", $"{(corpus.IsActive ? "Enabled" : "Disabled")}{(string.IsNullOrWhiteSpace(corpus.Description) ? string.Empty : $" · {corpus.Description}")}") { CorpusId = corpus.Id };
    private static KnowledgeOwnerSourceOption ToSourceOption(KnowledgeSource source) => new($"{source.Name} · {source.SourceType} · imported {source.ImportedAt.LocalDateTime:g}") { SourceId = source.Id };
    private static string? ValidateTenant(string? tenantId) => string.IsNullOrWhiteSpace(tenantId) ? "Your workspace is unavailable. Please refresh the page and try again." : null;
}
