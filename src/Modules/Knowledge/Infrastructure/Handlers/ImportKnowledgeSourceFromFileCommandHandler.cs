using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Owner;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class ImportKnowledgeSourceFromFileCommandHandler(
    IKnowledgeService knowledge,
    IKnowledgeImportFileReader fileReader,
    ILogger<ImportKnowledgeSourceFromFileCommandHandler> logger) : IRequestHandler<ImportKnowledgeSourceFromFileCommand, KnowledgeOwnerImportCommandResult>
{
    public async Task<KnowledgeOwnerImportCommandResult> Handle(ImportKnowledgeSourceFromFileCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateFileImport(request.Input);
        if (error is not null)
        {
            return KnowledgeOwnerImportCommandResult.Failure(error);
        }

        try
        {
            KnowledgeImportFile file = await fileReader.OpenAsync(request.Input.FileInformationId!.Value, request.TenantId!, cancellationToken);
            await using Stream content = file.Content;
            KnowledgeImportResult result = await knowledge.ImportAsync(BuildImportRequest(request.TenantId!, request.Input, file, content), cancellationToken);
            return KnowledgeOwnerImportCommandResult.Success("Import complete.", result.ToOwnerImportSummary());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FileNotFoundException ex)
        {
            logger.LogWarning(ex, "Knowledge file import could not find file {FileInformationId} for tenant {TenantId}.", request.Input.FileInformationId, request.TenantId);
            return KnowledgeOwnerImportCommandResult.Failure("The selected file is unavailable. Upload it again and retry.");
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Knowledge file import rejected file {FileInformationId} for tenant {TenantId}.", request.Input.FileInformationId, request.TenantId);
            return KnowledgeOwnerImportCommandResult.Failure("The selected file does not belong to this workspace.");
        }
        catch (NotSupportedException ex)
        {
            logger.LogWarning(ex, "Knowledge file import rejected unsupported format {FormatName}.", request.Input.FormatName);
            return KnowledgeOwnerImportCommandResult.Failure("Choose a supported content format.");
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "Knowledge file import rejected malformed content for file {FileInformationId}.", request.Input.FileInformationId);
            return KnowledgeOwnerImportCommandResult.Failure("The selected file content could not be imported.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import knowledge source from file {FileInformationId} for corpus {CorpusId} and tenant {TenantId}.", request.Input.FileInformationId, request.Input.CorpusId, request.TenantId);
            return KnowledgeOwnerImportCommandResult.Failure(KnowledgeOwnerFailureMessages.ImportFailed);
        }
    }

    internal static KnowledgeImportRequest BuildImportRequest(string tenantId, ImportKnowledgeSourceFromFileInput input, KnowledgeImportFile file, Stream content) => new()
    {
        CorpusId = input.CorpusId!.Value,
        TenantId = tenantId,
        Content = content,
        Format = input.FormatName.Trim(),
        SourceName = string.IsNullOrWhiteSpace(input.SourceName) ? file.DisplayName : input.SourceName.Trim(),
        OriginalLocation = file.OriginalLocation,
        Checksum = file.Checksum,
        ContentLength = file.Size
    };
}
