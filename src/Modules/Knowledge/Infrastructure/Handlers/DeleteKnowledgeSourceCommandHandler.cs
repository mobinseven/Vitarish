using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Extensions;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class DeleteKnowledgeSourceCommandHandler(IKnowledgeService knowledge, ILogger<DeleteKnowledgeSourceCommandHandler> logger) : IRequestHandler<DeleteKnowledgeSourceCommand, KnowledgeOwnerCommandResult>
{
    public async Task<KnowledgeOwnerCommandResult> Handle(DeleteKnowledgeSourceCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId)
            ?? KnowledgeOwnerInputValidator.ValidateOwner(request.RequestingOwnerId)
            ?? KnowledgeOwnerInputValidator.ValidateDeleteSource(request.Input);

        if (error is not null)
        {
            return KnowledgeOwnerCommandResult.Failure(error);
        }

        try
        {
            Guid corpusId = request.Input.CorpusId!.Value;
            Guid sourceId = request.Input.SourceId!.Value;
            KnowledgeCorpus? corpus = await knowledge.GetCorpusAsync(corpusId, request.TenantId!, cancellationToken);
            if (corpus is null)
            {
                return KnowledgeOwnerCommandResult.Failure("The selected corpus was not found.");
            }

            bool sourceExists = (await knowledge.GetSourcesAsync(corpus.Id, request.TenantId!, cancellationToken))
                .Any(source => source.Id == sourceId);
            if (!sourceExists)
            {
                return KnowledgeOwnerCommandResult.Failure("The selected source was not found.");
            }

            await knowledge.DeleteSourceAsync(sourceId, corpus.Id, request.TenantId!, cancellationToken);
            return KnowledgeOwnerCommandResult.Success("Source deleted.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to delete knowledge source {SourceId} from corpus {CorpusId} for tenant {TenantId} by owner {OwnerId}.", request.Input.SourceId, request.Input.CorpusId, request.TenantId, request.RequestingOwnerId);
            return KnowledgeOwnerCommandResult.Failure(KnowledgeOwnerFailureMessages.DeleteSourceFailed);
        }
    }
}
