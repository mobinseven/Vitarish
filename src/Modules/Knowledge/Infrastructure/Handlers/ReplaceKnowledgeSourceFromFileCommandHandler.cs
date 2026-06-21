using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Infrastructure.Extensions;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class ReplaceKnowledgeSourceFromFileCommandHandler(
    IKnowledgeService knowledge,
    IKnowledgeImportFileReader fileReader,
    ILogger<ReplaceKnowledgeSourceFromFileCommandHandler> logger) : IRequestHandler<ReplaceKnowledgeSourceFromFileCommand, KnowledgeOwnerImportCommandResult>
{
    public async Task<KnowledgeOwnerImportCommandResult> Handle(ReplaceKnowledgeSourceFromFileCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateFileReplace(request.Input);
        if (error is not null)
        {
            return KnowledgeOwnerImportCommandResult.Failure(error);
        }

        try
        {
            KnowledgeImportFile file = await fileReader.OpenAsync(request.Input.FileInformationId!.Value, request.TenantId!, cancellationToken);
            await using Stream content = file.Content;
            KnowledgeImportResult result = await knowledge.ReplaceSourceAsync(request.Input.SourceId!.Value, BuildImportRequest(request.TenantId!, request.Input, file, content), cancellationToken);
            return KnowledgeOwnerImportCommandResult.Success("Source replacement complete.", result.ToOwnerImportSummary());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FileNotFoundException ex)
        {
            logger.LogWarning(ex, "Knowledge file replacement could not find file {FileInformationId} for tenant {TenantId}.", request.Input.FileInformationId, request.TenantId);
            return KnowledgeOwnerImportCommandResult.Failure("The selected file is unavailable. Upload it again and retry.");
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Knowledge file replacement rejected file {FileInformationId} for tenant {TenantId}.", request.Input.FileInformationId, request.TenantId);
            return KnowledgeOwnerImportCommandResult.Failure("The selected file does not belong to this workspace.");
        }
        catch (NotSupportedException ex)
        {
            logger.LogWarning(ex, "Knowledge file replacement rejected unsupported format {FormatName}.", request.Input.Format);
            return KnowledgeOwnerImportCommandResult.Failure("Choose a supported content format.");
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "Knowledge file replacement rejected malformed content for file {FileInformationId}.", request.Input.FileInformationId);
            return KnowledgeOwnerImportCommandResult.Failure("The selected file content could not be imported.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to replace knowledge source {SourceId} from file {FileInformationId} for corpus {CorpusId} and tenant {TenantId}.", request.Input.SourceId, request.Input.FileInformationId, request.Input.CorpusId, request.TenantId);
            return KnowledgeOwnerImportCommandResult.Failure(KnowledgeOwnerFailureMessages.ReplaceFailed);
        }
    }

    private static KnowledgeImportRequest BuildImportRequest(string tenantId, ReplaceKnowledgeSourceFromFileInput input, KnowledgeImportFile file, Stream content) => new()
    {
        CorpusId = input.CorpusId!.Value,
        TenantId = tenantId,
        Content = content,
        Format = input.Format.ToImporterName(),
        SourceName = string.IsNullOrWhiteSpace(input.SourceName) ? file.DisplayName : input.SourceName.Trim(),
        OriginalLocation = file.OriginalLocation,
        Checksum = file.Checksum,
        ContentLength = file.Size
    };
}
