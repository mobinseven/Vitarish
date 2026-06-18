using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Owner;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class IndexKnowledgeCorpusCommandHandler(IKnowledgeService knowledge, ILogger<IndexKnowledgeCorpusCommandHandler> logger) : IRequestHandler<IndexKnowledgeCorpusCommand, KnowledgeOwnerCommandResult>
{
    public async Task<KnowledgeOwnerCommandResult> Handle(IndexKnowledgeCorpusCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateSelectedCorpus(request.Input.CorpusId);
        if (error is not null)
        {
            return KnowledgeOwnerCommandResult.Failure(error);
        }

        try
        {
            KnowledgeIndexResult result = await knowledge.IndexAsync(request.Input.CorpusId!.Value, request.TenantId!, cancellationToken);
            return KnowledgeOwnerCommandResult.Success(result.ToOwnerIndexSummary());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to index knowledge corpus {CorpusId} for tenant {TenantId}.", request.Input.CorpusId, request.TenantId);
            return KnowledgeOwnerCommandResult.Failure(KnowledgeOwnerFailureMessages.IndexFailed);
        }
    }
}
