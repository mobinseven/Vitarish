using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Infrastructure.Extensions;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class ReplaceKnowledgeSourceCommandHandler(IKnowledgeService knowledge, ILogger<ReplaceKnowledgeSourceCommandHandler> logger) : IRequestHandler<ReplaceKnowledgeSourceCommand, KnowledgeOwnerImportCommandResult>
{
    public async Task<KnowledgeOwnerImportCommandResult> Handle(ReplaceKnowledgeSourceCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateReplace(request.Input);
        if (error is not null)
        {
            return KnowledgeOwnerImportCommandResult.Failure(error);
        }

        try
        {
            await using MemoryStream content = new(Encoding.UTF8.GetBytes(request.Input.Content));
            KnowledgeImportResult result = await knowledge.ReplaceSourceAsync(request.Input.SourceId!.Value, ImportKnowledgeSourceCommandHandler.BuildImportRequest(request.TenantId!, request.Input.ToImportInput(), content), cancellationToken);
            return KnowledgeOwnerImportCommandResult.Success("Source replacement complete.", result.ToOwnerImportSummary());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to replace knowledge source {SourceId} for tenant {TenantId}.", request.Input.SourceId, request.TenantId);
            return KnowledgeOwnerImportCommandResult.Failure(KnowledgeOwnerFailureMessages.ReplaceFailed);
        }
    }
}
