using Vitarish.Entities.Server.Extensions;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Contracts.Routes;
using Vitarish.Files.Domain.Models;
using Vitarish.Parties.Application.Profiles;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Checks file references against the Files module. Its file lookups only see the request's
/// own tenant, so another tenant's file is indistinguishable from a missing one.
/// </summary>
public sealed class FilesTenantImageFiles(
    IFileInformationService files,
    IFileIntegrityService integrity,
    IObjectStorageService storage) : ITenantImageFiles
{
    // Images every browser shows; HEIC/HEIF uploads are accepted by Files but not shown everywhere.
    private static readonly HashSet<string> DisplayableImageTypes = new(
        FileTransferPolicy.UploadFormats
            .Where(format => format.Signature is FileSignature.Jpeg or FileSignature.Png or FileSignature.WebP)
            .Select(format => format.MediaType),
        StringComparer.OrdinalIgnoreCase);

    public async Task<bool> AreOwnImagesAsync(IReadOnlyCollection<Guid> fileIds, CancellationToken cancellationToken = default)
    {
        foreach (Guid fileId in fileIds)
        {
            FileInformationReadDto? file = await files.GetAsync(fileId, cancellationToken).GetValueOrDefaultAsync();
            if (file is null || !DisplayableImageTypes.Contains(file.ContentType))
                return false;
        }
        return true;
    }

    public string ContentUrl(Guid fileId) => FilesApiRoutes.ContentPath(fileId);

    public async Task<TenantImageContent?> OpenAsync(string contentUrl, CancellationToken cancellationToken = default)
    {
        if (ContentFileId(contentUrl) is not { } fileId)
            return null;
        FileInformationReadDto? file = await files.GetAsync(fileId, cancellationToken).GetValueOrDefaultAsync();
        if (file is null || !DisplayableImageTypes.Contains(file.ContentType) || string.IsNullOrWhiteSpace(file.Checksum))
            return null;

        FileIntegrityVerificationResult verification = await integrity.VerifyAsync(file, cancellationToken);
        if (!verification.IsVerified || string.IsNullOrWhiteSpace(verification.ObjectKey))
            return null;

        Stream content = await storage.DownloadAsync(verification.ObjectKey, cancellationToken);
        return new TenantImageContent(content, file.ContentType, verification.ExpectedLength);
    }

    // Only an address ContentUrl made names a file; anything else stored as a logo is not one.
    private static Guid? ContentFileId(string contentUrl) =>
        contentUrl.Split('/')
            .Select(segment => Guid.TryParse(segment, out Guid fileId) ? fileId : (Guid?)null)
            .FirstOrDefault(fileId => fileId is { } id && FilesApiRoutes.ContentPath(id) == contentUrl);
}
