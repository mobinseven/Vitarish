using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Files.Application.Access;

/// <summary>
/// Authorizes direct Files metadata operations using only trusted request context.
/// </summary>
public sealed class FileMetadataAccess(
    IFileInformationService fileInformationService,
    IRequestContext requestContext) : IFileMetadataAccess
{
    public async Task<FileInformationReadDto?> GetAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        if (!HasTrustedSession())
        {
            return null;
        }

        FileInformationReadDto? file = await fileInformationService
            .GetAsync(fileId, cancellationToken)
            .GetValueOrDefaultAsync()
            .ConfigureAwait(false);
        return IsOwnedByCurrentTenant(file) ? file : null;
    }

    public async Task<IReadOnlyList<FileInformationReadDto>> ListAsync(
        FileInformationDescriptorDto descriptor,
        CancellationToken cancellationToken = default)
    {
        if (!HasTrustedSession())
        {
            return [];
        }

        FileInformationDescriptorDto tenantDescriptor = descriptor with { Owner = requestContext.TenantId };
        return await fileInformationService
            .GetListAsync(tenantDescriptor, cancellationToken)
            .GetValueOrDefaultAsync()
            .ConfigureAwait(false) ?? [];
    }

    public async Task<FileInformationReadDto?> CreateAsync(
        FileInformationWriteDto file,
        CancellationToken cancellationToken = default)
    {
        if (!HasTrustedSession())
        {
            return null;
        }

        FileInformationWriteDto tenantFile = file with { Owner = requestContext.TenantId };
        return await fileInformationService
            .AddAsync(tenantFile, cancellationToken)
            .GetValueOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task<FileInformationReadDto?> AuthorizeDownloadAsync(
        Guid fileId,
        CancellationToken cancellationToken = default) => await GetAsync(fileId, cancellationToken).ConfigureAwait(false);

    private bool HasTrustedSession() =>
        !string.IsNullOrWhiteSpace(requestContext.TenantId) &&
        !string.IsNullOrWhiteSpace(requestContext.UserId);

    private bool IsOwnedByCurrentTenant(FileInformationReadDto? file) =>
        file is not null &&
        string.Equals(file.Owner, requestContext.TenantId, StringComparison.Ordinal);
}
