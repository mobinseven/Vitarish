using Microsoft.Extensions.Logging;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Models;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Sea;

/// <summary>
/// Processes migration of Attachments from URL-based content references to ID-based references.
/// </summary>
public class AttachmentMigrationProcessor(
    ILogger<AttachmentMigrationProcessor> logger,
    TenantBackgroundExecutor tenantBackgroundExecutor)
{
    private const string BackgroundResource = "hangfire:sea:attachments:migration";

    /// <summary>
    /// Scans all Attachments and migrates those with URL-based content to ID-based content.
    /// </summary>
    public async Task ProcessMigration(CancellationToken cancellationToken = default)
    {
        async Task perTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
        {
            IEventsService eventsService = provider.GetRequiredService<IEventsService>();
            IAttachmentsService attachmentsService = provider.GetRequiredService<IAttachmentsService>();
            IFileInformationService fileInformationService = provider.GetRequiredService<IFileInformationService>();
            IRequestContext requestContext = provider.GetRequiredService<IRequestContext>();

            await ProcessTenantMigrationAsync(eventsService, attachmentsService, fileInformationService, requestContext, shutdownToken);
        }

        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            perTenantAsync,
            cancellationToken);
    }

    private async Task ProcessTenantMigrationAsync(
        IEventsService eventsService,
        IAttachmentsService attachmentsService,
        IFileInformationService fileInformationService,
        IRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? "unknown";
        logger.LogInformation("Starting Attachment URL to ID migration for tenant {TenantId}", tenantId);

        try
        {
            int migratedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;
            int processedEventCount = 0;

            // Get all events in batches and process them
            const int batchSize = 1000;
            int skip = 0;
            bool hasMoreEvents = true;

            while (hasMoreEvents)
            {
                EventDescriptorDto eventDescriptor = new()
                {
                    Skip = skip,
                    Take = batchSize
                };

                var eventsResult = await eventsService.GetListAsync(eventDescriptor, cancellationToken);
                IReadOnlyList<EventDto>? events = eventsResult.GetValueOrDefault();

                if (events == null || events.Count == 0)
                {
                    hasMoreEvents = false;
                    break;
                }

                logger.LogDebug("Processing batch of {Count} events (skip: {Skip}) for tenant {TenantId}", 
                    events.Count, skip, tenantId);

                processedEventCount += events.Count;

                foreach (EventDto eventDto in events)
                {
                    try
                    {
                        // Get all attachments for this event
                        IReadOnlyList<AttachmentDto> attachments = await attachmentsService.GetByEventAsync(eventDto.Id, cancellationToken);

                        foreach (AttachmentDto attachment in attachments)
                        {
                            try
                            {
                                // Skip if already migrated (content is a valid GUID)
                                if (Guid.TryParse(attachment.Content, out _))
                                {
                                    skippedCount++;
                                    continue;
                                }

                                // Skip text attachments (they store actual text content, not file references)
                                // Text attachments are identified by content types starting with "text/"
                                // Other content types (images, videos, documents) store file references that need migration
                                if (attachment.ContentType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    skippedCount++;
                                    continue;
                                }

                                // Try to find the corresponding FileInformation by URL
                                FileInformationDescriptorDto fileDescriptor = new()
                                {
                                    Url = attachment.Content
                                };

                                var fileInfoResult = await fileInformationService.GetListAsync(fileDescriptor, cancellationToken);
                                IReadOnlyList<FileInformationDto>? fileInfos = fileInfoResult.GetValueOrDefault();

                                if (fileInfos?.Count > 0)
                                {
                                    // Warn if multiple files match the same URL (potential data inconsistency)
                                    if (fileInfos.Count > 1)
                                    {
                                        logger.LogWarning(
                                            "Multiple FileInformation records ({Count}) found for URL '{Url}' on attachment {AttachmentId} for tenant {TenantId}. Using the first match.",
                                            fileInfos.Count,
                                            attachment.Content,
                                            attachment.Id,
                                            tenantId);
                                    }

                                    FileInformationDto fileInfo = fileInfos[0];

                                    // Update the attachment content to use the file ID
                                    AttachmentDto? updated = await attachmentsService.UpdateAsync(
                                        eventDto.Id,
                                        attachment.Id,
                                        fileInfo.Id.ToString(),
                                        cancellationToken);

                                    if (updated != null)
                                    {
                                        migratedCount++;

                                        logger.LogDebug(
                                            "Migrated attachment {AttachmentId}: URL '{Url}' -> ID '{FileId}' for tenant {TenantId}",
                                            attachment.Id,
                                            fileInfo.Url,
                                            fileInfo.Id,
                                            tenantId);
                                    }
                                    else
                                    {
                                        logger.LogWarning(
                                            "Failed to update attachment {AttachmentId} for tenant {TenantId}",
                                            attachment.Id,
                                            tenantId);
                                        errorCount++;
                                    }
                                }
                                else
                                {
                                    logger.LogWarning(
                                        "Could not find FileInformation for attachment {AttachmentId} with URL: {Url} for tenant {TenantId}",
                                        attachment.Id,
                                        attachment.Content,
                                        tenantId);
                                    errorCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(
                                    ex,
                                    "Error processing attachment {AttachmentId} for tenant {TenantId}",
                                    attachment.Id,
                                    tenantId);
                                errorCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Error processing event {EventId} for tenant {TenantId}",
                            eventDto.Id,
                            tenantId);
                    }
                }

                // Move to next batch
                skip += batchSize;
                hasMoreEvents = events.Count == batchSize; // If we got fewer than batchSize, we're done
            }

            logger.LogInformation(
                "Attachment migration completed for tenant {TenantId}. Events processed: {EventCount}, Migrated: {Migrated}, Skipped: {Skipped}, Errors: {Errors}",
                tenantId,
                processedEventCount,
                migratedCount,
                skippedCount,
                errorCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error during Attachment migration for tenant {TenantId}", tenantId);
            throw;
        }
    }
}
