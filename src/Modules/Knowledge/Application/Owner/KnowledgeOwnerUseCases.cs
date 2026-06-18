using System.Text;
using MediatR;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Owner;

public sealed record KnowledgeOwnerReadModelQuery(string? TenantId, Guid? SelectedCorpusId) : IRequest<KnowledgeOwnerReadModel>;
public sealed record CreateKnowledgeCorpusCommand(string? TenantId, CreateKnowledgeCorpusInput Input) : IRequest<KnowledgeOwnerCreateCorpusResult>;
public sealed record ImportKnowledgeSourceCommand(string? TenantId, ImportKnowledgeSourceInput Input) : IRequest<KnowledgeOwnerImportCommandResult>;
public sealed record ReplaceKnowledgeSourceCommand(string? TenantId, ReplaceKnowledgeSourceInput Input) : IRequest<KnowledgeOwnerImportCommandResult>;
public sealed record IndexKnowledgeCorpusCommand(string? TenantId, IndexKnowledgeCorpusInput Input) : IRequest<KnowledgeOwnerCommandResult>;
public sealed record EvaluateKnowledgeCorpusCommand(string? TenantId, EvaluateKnowledgeCorpusInput Input) : IRequest<KnowledgeOwnerEvaluationCommandResult>;

public sealed class KnowledgeOwnerReadModelQueryHandler(IKnowledgeService knowledge) : IRequestHandler<KnowledgeOwnerReadModelQuery, KnowledgeOwnerReadModel>
{
    public async Task<KnowledgeOwnerReadModel> Handle(KnowledgeOwnerReadModelQuery request, CancellationToken cancellationToken)
    {
        string? tenantError = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId);
        if (tenantError is not null) return KnowledgeOwnerReadModel.Empty;

        IReadOnlyList<KnowledgeCorpus> corpora = await knowledge.ListCorporaAsync(request.TenantId!, cancellationToken);
        IReadOnlyList<KnowledgeOwnerCorpusOption> corpusOptions = corpora.ToOwnerCorpusOptions();
        KnowledgeOwnerCorpusOption? selected = corpusOptions.ResolveSelectedOwnerCorpus(request.SelectedCorpusId);
        IReadOnlyList<KnowledgeSource> sources = selected is null ? [] : await knowledge.GetSourcesAsync(selected.CorpusId, request.TenantId!, cancellationToken);

        return corpusOptions.ToOwnerReadModel(selected, sources);
    }
}

public sealed class CreateKnowledgeCorpusCommandHandler(IKnowledgeService knowledge) : IRequestHandler<CreateKnowledgeCorpusCommand, KnowledgeOwnerCreateCorpusResult>
{
    public async Task<KnowledgeOwnerCreateCorpusResult> Handle(CreateKnowledgeCorpusCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateCreateCorpus(request.Input);
        if (error is not null) return KnowledgeOwnerCreateCorpusResult.Failure(error);

        KnowledgeCorpus corpus = new() { Id = Guid.NewGuid(), TenantId = request.TenantId!, Name = request.Input.Name.Trim(), Description = string.IsNullOrWhiteSpace(request.Input.Description) ? null : request.Input.Description.Trim(), IsActive = request.Input.IsActive };
        try
        {
            await knowledge.SaveCorpusAsync(corpus, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return KnowledgeOwnerCreateCorpusResult.Failure(KnowledgeOwnerFailureMessages.SaveCorpusFailed);
        }

        return KnowledgeOwnerCreateCorpusResult.Success("Corpus created.", corpus.ToOwnerCorpusOption());
    }
}

public sealed class ImportKnowledgeSourceCommandHandler(IKnowledgeService knowledge) : IRequestHandler<ImportKnowledgeSourceCommand, KnowledgeOwnerImportCommandResult>
{
    public async Task<KnowledgeOwnerImportCommandResult> Handle(ImportKnowledgeSourceCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateImport(request.Input);
        if (error is not null) return KnowledgeOwnerImportCommandResult.Failure(error);

        try
        {
            await using MemoryStream content = new(Encoding.UTF8.GetBytes(request.Input.Content));
            KnowledgeImportResult result = await knowledge.ImportAsync(BuildImportRequest(request.TenantId!, request.Input, content), cancellationToken);
            return KnowledgeOwnerImportCommandResult.Success("Import complete.", result.ToOwnerImportSummary());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return KnowledgeOwnerImportCommandResult.Failure(KnowledgeOwnerFailureMessages.ImportFailed);
        }
    }

    internal static KnowledgeImportRequest BuildImportRequest(string tenantId, ImportKnowledgeSourceInput input, Stream content) => new() { CorpusId = input.CorpusId!.Value, TenantId = tenantId, Content = content, Format = input.FormatName.Trim(), SourceName = string.IsNullOrWhiteSpace(input.SourceName) ? null : input.SourceName.Trim() };
}

public sealed class ReplaceKnowledgeSourceCommandHandler(IKnowledgeService knowledge) : IRequestHandler<ReplaceKnowledgeSourceCommand, KnowledgeOwnerImportCommandResult>
{
    public async Task<KnowledgeOwnerImportCommandResult> Handle(ReplaceKnowledgeSourceCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateReplace(request.Input);
        if (error is not null) return KnowledgeOwnerImportCommandResult.Failure(error);

        try
        {
            await using MemoryStream content = new(Encoding.UTF8.GetBytes(request.Input.Content));
            KnowledgeImportResult result = await knowledge.ReplaceSourceAsync(request.Input.SourceId!.Value, ImportKnowledgeSourceCommandHandler.BuildImportRequest(request.TenantId!, request.Input.ToImportInput(), content), cancellationToken);
            return KnowledgeOwnerImportCommandResult.Success("Source replacement complete.", result.ToOwnerImportSummary());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return KnowledgeOwnerImportCommandResult.Failure(KnowledgeOwnerFailureMessages.ReplaceFailed);
        }
    }
}

public sealed class IndexKnowledgeCorpusCommandHandler(IKnowledgeService knowledge) : IRequestHandler<IndexKnowledgeCorpusCommand, KnowledgeOwnerCommandResult>
{
    public async Task<KnowledgeOwnerCommandResult> Handle(IndexKnowledgeCorpusCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateSelectedCorpus(request.Input.CorpusId);
        if (error is not null) return KnowledgeOwnerCommandResult.Failure(error);

        try
        {
            KnowledgeIndexResult result = await knowledge.IndexAsync(request.Input.CorpusId!.Value, request.TenantId!, cancellationToken);
            return KnowledgeOwnerCommandResult.Success(result.ToOwnerIndexSummary());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return KnowledgeOwnerCommandResult.Failure(KnowledgeOwnerFailureMessages.IndexFailed);
        }
    }
}

public sealed class EvaluateKnowledgeCorpusCommandHandler(IKnowledgeService knowledge) : IRequestHandler<EvaluateKnowledgeCorpusCommand, KnowledgeOwnerEvaluationCommandResult>
{
    public async Task<KnowledgeOwnerEvaluationCommandResult> Handle(EvaluateKnowledgeCorpusCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateSelectedCorpus(request.Input.CorpusId);
        if (error is not null) return KnowledgeOwnerEvaluationCommandResult.Failure(error);

        try
        {
            KnowledgeEvaluationRun run = await knowledge.EvaluateAsync(new KnowledgeEvaluationRequest(request.Input.CorpusId!.Value, request.TenantId!), cancellationToken);
            return KnowledgeOwnerEvaluationCommandResult.Success("Evaluation complete.", run.ToOwnerEvaluationSummary());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return KnowledgeOwnerEvaluationCommandResult.Failure(KnowledgeOwnerFailureMessages.EvaluationFailed);
        }
    }
}
