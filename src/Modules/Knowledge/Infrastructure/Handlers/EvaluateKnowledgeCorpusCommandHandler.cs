using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Owner;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class EvaluateKnowledgeCorpusCommandHandler(IKnowledgeService knowledge, ILogger<EvaluateKnowledgeCorpusCommandHandler> logger) : IRequestHandler<EvaluateKnowledgeCorpusCommand, KnowledgeOwnerEvaluationCommandResult>
{
    public async Task<KnowledgeOwnerEvaluationCommandResult> Handle(EvaluateKnowledgeCorpusCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateSelectedCorpus(request.Input.CorpusId);
        if (error is not null)
        {
            return KnowledgeOwnerEvaluationCommandResult.Failure(error);
        }

        try
        {
            KnowledgeEvaluationRun run = await knowledge.EvaluateAsync(new KnowledgeEvaluationRequest(request.Input.CorpusId!.Value, request.TenantId!), cancellationToken);
            return KnowledgeOwnerEvaluationCommandResult.Success("Evaluation complete.", run.ToOwnerEvaluationSummary());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to evaluate knowledge corpus {CorpusId} for tenant {TenantId}.", request.Input.CorpusId, request.TenantId);
            return KnowledgeOwnerEvaluationCommandResult.Failure(KnowledgeOwnerFailureMessages.EvaluationFailed);
        }
    }
}
