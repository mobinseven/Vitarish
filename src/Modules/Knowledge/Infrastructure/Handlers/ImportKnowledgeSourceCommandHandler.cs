using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Infrastructure.Extensions;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class ImportKnowledgeSourceCommandHandler(IKnowledgeService knowledge, ILogger<ImportKnowledgeSourceCommandHandler> logger) : IRequestHandler<ImportKnowledgeSourceCommand, KnowledgeOwnerImportCommandResult>
{
    public async Task<KnowledgeOwnerImportCommandResult> Handle(ImportKnowledgeSourceCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateImport(request.Input);
        if (error is not null)
        {
            return KnowledgeOwnerImportCommandResult.Failure(error);
        }

        try
        {
            await using MemoryStream content = new(Encoding.UTF8.GetBytes(request.Input.Content));
            KnowledgeImportResult result = await knowledge.ImportAsync(BuildImportRequest(request.TenantId!, request.Input, content), cancellationToken);
            return KnowledgeOwnerImportCommandResult.Success("Import complete.", result.ToOwnerImportSummary());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to import knowledge source for corpus {CorpusId} and tenant {TenantId}.", request.Input.CorpusId, request.TenantId);
            return KnowledgeOwnerImportCommandResult.Failure(KnowledgeOwnerFailureMessages.ImportFailed);
        }
    }

    internal static KnowledgeImportRequest BuildImportRequest(string tenantId, ImportKnowledgeSourceInput input, Stream content) => new()
    {
        CorpusId = input.CorpusId!.Value,
        TenantId = tenantId,
        Content = content,
        Format = input.Format.ToImporterName(),
        SourceName = string.IsNullOrWhiteSpace(input.SourceName) ? null : input.SourceName.Trim()
    };
}
