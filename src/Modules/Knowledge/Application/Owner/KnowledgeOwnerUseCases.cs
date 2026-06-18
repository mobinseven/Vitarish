using System.Text;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Owner;

public interface IKnowledgeOwnerReadModelQuery
{
    Task<KnowledgeOwnerReadModel> LoadAsync(string? tenantId, Guid? selectedCorpusId, CancellationToken cancellationToken);
}

public interface ICreateKnowledgeCorpusCommand
{
    Task<KnowledgeOwnerCreateCorpusResult> CreateCorpusAsync(string? tenantId, CreateKnowledgeCorpusInput input, CancellationToken cancellationToken);
}

public interface IImportKnowledgeSourceCommand
{
    KnowledgeOwnerImportFormats ImportFormats { get; }
    Task<KnowledgeOwnerImportCommandResult> ImportSourceAsync(string? tenantId, ImportKnowledgeSourceInput input, CancellationToken cancellationToken);
}

public interface IReplaceKnowledgeSourceCommand
{
    KnowledgeOwnerImportFormats ImportFormats { get; }
    Task<KnowledgeOwnerImportCommandResult> ReplaceSourceAsync(string? tenantId, ReplaceKnowledgeSourceInput input, CancellationToken cancellationToken);
}

public interface IIndexKnowledgeCorpusCommand
{
    Task<KnowledgeOwnerCommandResult> IndexCorpusAsync(string? tenantId, IndexKnowledgeCorpusInput input, CancellationToken cancellationToken);
}

public interface IEvaluateKnowledgeCorpusCommand
{
    Task<KnowledgeOwnerEvaluationCommandResult> EvaluateCorpusAsync(string? tenantId, EvaluateKnowledgeCorpusInput input, CancellationToken cancellationToken);
}

public sealed class KnowledgeOwnerReadModelQuery(IKnowledgeService knowledge, KnowledgeOwnerReadModelProjector projector) : IKnowledgeOwnerReadModelQuery
{
    public async Task<KnowledgeOwnerReadModel> LoadAsync(string? tenantId, Guid? selectedCorpusId, CancellationToken cancellationToken)
    {
        string? tenantError = KnowledgeOwnerInputValidator.ValidateTenant(tenantId);
        if (tenantError is not null) return KnowledgeOwnerReadModel.Empty;

        IReadOnlyList<KnowledgeCorpus> corpora = await knowledge.ListCorporaAsync(tenantId!, cancellationToken);
        IReadOnlyList<KnowledgeOwnerCorpusOption> corpusOptions = projector.ToCorpusOptions(corpora);
        KnowledgeOwnerCorpusOption? selected = projector.ResolveSelectedCorpus(corpusOptions, selectedCorpusId);
        IReadOnlyList<KnowledgeSource> sources = selected is null ? [] : await knowledge.GetSourcesAsync(selected.CorpusId, tenantId!, cancellationToken);

        return projector.ToReadModel(corpusOptions, selected, sources);
    }
}

public sealed class CreateKnowledgeCorpusCommand(IKnowledgeService knowledge, KnowledgeOwnerReadModelProjector projector) : ICreateKnowledgeCorpusCommand
{
    public async Task<KnowledgeOwnerCreateCorpusResult> CreateCorpusAsync(string? tenantId, CreateKnowledgeCorpusInput input, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(tenantId) ?? KnowledgeOwnerInputValidator.ValidateCreateCorpus(input);
        if (error is not null) return KnowledgeOwnerCreateCorpusResult.Failure(error);

        KnowledgeCorpus corpus = new() { Id = Guid.NewGuid(), TenantId = tenantId!, Name = input.Name.Trim(), Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(), IsActive = input.IsActive };
        try
        {
            await knowledge.SaveCorpusAsync(corpus, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return KnowledgeOwnerCreateCorpusResult.Failure(KnowledgeOwnerFailureMessages.SaveCorpusFailed);
        }

        return KnowledgeOwnerCreateCorpusResult.Success("Corpus created.", projector.ToCorpusOption(corpus));
    }
}

public sealed class ImportKnowledgeSourceCommand(IKnowledgeService knowledge, KnowledgeOwnerResultFormatter formatter) : IImportKnowledgeSourceCommand
{
    public KnowledgeOwnerImportFormats ImportFormats => KnowledgeOwnerImportFormatCatalog.Current;

    public async Task<KnowledgeOwnerImportCommandResult> ImportSourceAsync(string? tenantId, ImportKnowledgeSourceInput input, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(tenantId) ?? KnowledgeOwnerInputValidator.ValidateImport(input);
        if (error is not null) return KnowledgeOwnerImportCommandResult.Failure(error);

        try
        {
            await using MemoryStream content = new(Encoding.UTF8.GetBytes(input.Content));
            KnowledgeImportResult result = await knowledge.ImportAsync(BuildImportRequest(tenantId!, input, content), cancellationToken);
            return KnowledgeOwnerImportCommandResult.Success("Import complete.", formatter.FormatImportSummary(result));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return KnowledgeOwnerImportCommandResult.Failure(KnowledgeOwnerFailureMessages.ImportFailed);
        }
    }

    internal static KnowledgeImportRequest BuildImportRequest(string tenantId, ImportKnowledgeSourceInput input, Stream content) => new() { CorpusId = input.CorpusId!.Value, TenantId = tenantId, Content = content, Format = input.FormatName.Trim(), SourceName = string.IsNullOrWhiteSpace(input.SourceName) ? null : input.SourceName.Trim() };
}

public sealed class ReplaceKnowledgeSourceCommand(IKnowledgeService knowledge, KnowledgeOwnerResultFormatter formatter) : IReplaceKnowledgeSourceCommand
{
    public KnowledgeOwnerImportFormats ImportFormats => KnowledgeOwnerImportFormatCatalog.Current;

    public async Task<KnowledgeOwnerImportCommandResult> ReplaceSourceAsync(string? tenantId, ReplaceKnowledgeSourceInput input, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(tenantId) ?? KnowledgeOwnerInputValidator.ValidateReplace(input);
        if (error is not null) return KnowledgeOwnerImportCommandResult.Failure(error);

        try
        {
            await using MemoryStream content = new(Encoding.UTF8.GetBytes(input.Content));
            KnowledgeImportResult result = await knowledge.ReplaceSourceAsync(input.SourceId!.Value, ImportKnowledgeSourceCommand.BuildImportRequest(tenantId!, input.ToImportInput(), content), cancellationToken);
            return KnowledgeOwnerImportCommandResult.Success("Source replacement complete.", formatter.FormatImportSummary(result));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return KnowledgeOwnerImportCommandResult.Failure(KnowledgeOwnerFailureMessages.ReplaceFailed);
        }
    }
}

public sealed class IndexKnowledgeCorpusCommand(IKnowledgeService knowledge, KnowledgeOwnerResultFormatter formatter) : IIndexKnowledgeCorpusCommand
{
    public async Task<KnowledgeOwnerCommandResult> IndexCorpusAsync(string? tenantId, IndexKnowledgeCorpusInput input, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(tenantId) ?? KnowledgeOwnerInputValidator.ValidateSelectedCorpus(input.CorpusId);
        if (error is not null) return KnowledgeOwnerCommandResult.Failure(error);

        try
        {
            KnowledgeIndexResult result = await knowledge.IndexAsync(input.CorpusId!.Value, tenantId!, cancellationToken);
            return KnowledgeOwnerCommandResult.Success(formatter.FormatIndexSummary(result));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return KnowledgeOwnerCommandResult.Failure(KnowledgeOwnerFailureMessages.IndexFailed);
        }
    }
}

public sealed class EvaluateKnowledgeCorpusCommand(IKnowledgeService knowledge, KnowledgeOwnerResultFormatter formatter) : IEvaluateKnowledgeCorpusCommand
{
    public async Task<KnowledgeOwnerEvaluationCommandResult> EvaluateCorpusAsync(string? tenantId, EvaluateKnowledgeCorpusInput input, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(tenantId) ?? KnowledgeOwnerInputValidator.ValidateSelectedCorpus(input.CorpusId);
        if (error is not null) return KnowledgeOwnerEvaluationCommandResult.Failure(error);

        try
        {
            KnowledgeEvaluationRun run = await knowledge.EvaluateAsync(new KnowledgeEvaluationRequest(input.CorpusId!.Value, tenantId!), cancellationToken);
            return KnowledgeOwnerEvaluationCommandResult.Success("Evaluation complete.", formatter.FormatEvaluationSummary(run));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return KnowledgeOwnerEvaluationCommandResult.Failure(KnowledgeOwnerFailureMessages.EvaluationFailed);
        }
    }
}
