using Vitarish.Entities.Server.Extensions;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Infrastructure.Services.Importing;

public sealed class KnowledgeImportFileReader(
    IFileInformationService fileInformationService,
    IFileIntegrityService integrityService,
    IObjectStorageService storage) : IKnowledgeImportFileReader
{
    public async Task<KnowledgeImportFile> OpenAsync(Guid fileInformationId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        FileInformationReadDto file = await fileInformationService.GetAsync(fileInformationId, cancellationToken).GetValueOrDefaultAsync()
            ?? throw new FileNotFoundException($"File information '{fileInformationId}' was not found.");

        if (!string.Equals(file.Owner, tenantId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException($"File information '{fileInformationId}' is not available for tenant '{tenantId}'.");
        }

        if (string.IsNullOrWhiteSpace(file.Checksum))
        {
            throw new InvalidDataException($"File information '{fileInformationId}' does not have a checksum.");
        }

        FileIntegrityVerificationResult verification = await integrityService.VerifyAsync(file, cancellationToken).ConfigureAwait(false);
        if (!verification.IsVerified || string.IsNullOrWhiteSpace(verification.ObjectKey))
        {
            throw new FileNotFoundException($"Stored object for file information '{fileInformationId}' was not found.");
        }

        Stream content = await storage.DownloadAsync(verification.ObjectKey, cancellationToken).ConfigureAwait(false);
        string displayName = string.IsNullOrWhiteSpace(file.Tag) ? fileInformationId.ToString() : file.Tag;
        return new KnowledgeImportFile(content, displayName, file.Checksum, $"file:{fileInformationId}", verification.ExpectedContentType, verification.ExpectedLength);
    }
}
