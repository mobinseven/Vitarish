using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;
using Vitarish.Sea.Infrastructure.Data;

namespace Vitarish.Sea.Infrastructure.SearchProviders;

/// <summary>
/// Exposes textual Sea attachments for full-text indexing via the Search module.
/// Only attachments with a text-based content type (e.g. text/plain, text/csv) are indexed;
/// binary attachments are skipped. File content is fetched from object storage and truncated
/// to 8 KB to keep index documents at a manageable size.
/// </summary>
internal sealed class SeaAttachmentsSearchProvider(
    SeaDbContext dbContext,
    IFileInformationService fileInformationService,
    IObjectStorageService objectStorageService,
    ILogger<SeaAttachmentsSearchProvider> logger) : ISearchIndexProvider
{
    private const int MaxBodyBytes = 8 * 1024;

    public string IndexName => "sea.attachments";

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var textAttachments = await dbContext.Attachments
            .AsNoTracking()
            .Where(a => a.ContentType.StartsWith("text/"))
            .Select(a => new { a.Id, a.EventId, a.Content, a.ContentType })
            .ToListAsync(cancellationToken);

        List<SearchDocument> documents = new(textAttachments.Count);

        foreach (var attachment in textAttachments)
        {
            if (!Guid.TryParse(attachment.Content, out Guid fileId))
            {
                // Legacy URL-based reference — skip until migrated
                continue;
            }

            try
            {
                FileInformationReadDto? fileInfo = await fileInformationService
                    .GetAsync(fileId, cancellationToken)
                    .GetValueOrDefaultAsync();

                if (fileInfo is null)
                {
                    logger.LogWarning(
                        "Attachment {AttachmentId} references file {FileId} that was not found in the Files service; skipping.",
                        attachment.Id, fileId);
                    continue;
                }

                await using Stream fileStream = await objectStorageService.DownloadAsync(
                    fileInfo.Checksum, cancellationToken);

                string body = await ReadTruncatedTextAsync(fileStream, MaxBodyBytes, cancellationToken);

                string title = string.IsNullOrWhiteSpace(fileInfo.Metadata.Extension)
                    ? attachment.ContentType
                    : $"Attachment (.{fileInfo.Metadata.Extension})";

                documents.Add(new SearchDocument
                {
                    Id = attachment.Id.ToString(),
                    Title = title,
                    Body = body,
                    Type = "attachment",
                    NavigationUrl = $"/sea/attachments/{attachment.Id}",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["eventId"] = attachment.EventId,
                        ["fileId"] = fileId
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to index attachment {AttachmentId} (file {FileId}); skipping.",
                    attachment.Id, fileId);
            }
        }

        return documents;
    }

    private static async Task<string> ReadTruncatedTextAsync(
        Stream stream,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[maxBytes];
        int totalRead = 0;

        while (totalRead < maxBytes)
        {
            int read = await stream.ReadAsync(
                buffer.AsMemory(totalRead, maxBytes - totalRead),
                cancellationToken);

            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return System.Text.Encoding.UTF8.GetString(buffer, 0, totalRead);
    }
}
