using Microsoft.Extensions.Logging;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Models;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Sea;

/// <summary>
/// Processes migration of Attachments from URL-based content references to ID-based references.
/// </summary>
public class AttachmentMigrationProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor)
{
    private const string BackgroundResource = "hangfire:sea:attachments:migration";

    /// <summary>
    /// Scans all Attachments and migrates those with URL-based content to ID-based content.
    /// </summary>
    public async Task ProcessMigration(CancellationToken cancellationToken = default)
    {
        static async Task perTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
        {
            await ProcessTenantMigrationAsync(
                provider.GetRequiredService<IOptions<FilesOptions>>(),
                provider.GetRequiredService<IEventsService>(),
                provider.GetRequiredService<IAttachmentsService>(),
                provider.GetRequiredService<IRequestContext>(),
                shutdownToken);
        }

        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            perTenantAsync,
            cancellationToken);
    }

    private static async Task ProcessTenantMigrationAsync(
        IOptions<FilesOptions> options,
        IEventsService eventsService,
        IAttachmentsService attachmentsService,
        IRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        const int batchSize = 1000;
        int skip = 0;
        bool hasMoreEvents = true;

        while (hasMoreEvents)
        {
            EventDescriptorDto eventDescriptor = new()
            {
                Skip = skip,
                DeletedTo = DateTime.Now,
                Take = batchSize
            };

            var eventsResult = await eventsService.GetListAsync(eventDescriptor, cancellationToken);
            IReadOnlyList<EventDto>? events = eventsResult.GetValueOrDefault();

            if (events == null || events.Count == 0)
            {
                hasMoreEvents = false;
                break;
            }

            foreach (EventDto eventDto in events)
            {
                IReadOnlyList<AttachmentDto> attachments = await attachmentsService.GetListAsync(new AttachmentDescriptorDto
                {
                    EventId = eventDto.Id,
                    DeletedTo = DateTime.Now,
                }, cancellationToken).GetValueOrDefaultAsync() ?? [];

                foreach (AttachmentDto attachment in attachments)
                {
                    if (!attachment.Content.StartsWith("http"))
                    {
                        continue;
                    }

                    //string id = FilesExtensions.ExtractId(attachment.Content, "VITARISH");

                    //AttachmentDto? updated = await attachmentsService.UpdateAsync(
                    //    eventDto.Id,
                    //    attachment.Id,
                    //    id,
                    //    cancellationToken);
                }
            }

            // Move to next batch
            skip += batchSize;
            hasMoreEvents = events.Count == batchSize; // If we got fewer than batchSize, we're done
        }
    }
}
