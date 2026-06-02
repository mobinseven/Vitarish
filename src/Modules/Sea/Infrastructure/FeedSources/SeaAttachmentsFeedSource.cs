using Vitarish.Feed.Domain.Abstractions;
using Vitarish.Feed.Domain.Models;

namespace Vitarish.Sea.Infrastructure.FeedSources;

/// <summary>
/// Feed data source that provides recent attachments from the Sea block
/// </summary>
public class SeaAttachmentsFeedSource(IAttachmentsService attachmentsService) : IFeedDataSource
{
    public string SourceId => "sea.attachments";
    public string SourceName => "Sea Attachments";
    public string Description => "Recent file attachments from Sea projects";
    public int Priority => 10;
    public bool EnabledByDefault => true;

    public async Task<IReadOnlyList<FeedItem>> GetFeedItemsAsync(
        FeedQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Query recent attachments across all events
            // Note: This is a simplified example. In a real implementation,
            // you'd want a dedicated query for recent attachments
            List<FeedItem> feedItems = [];

            // For now, return empty list as placeholder
            // A proper implementation would query the database for recent attachments
            // Example:
            // var recentAttachments = await _mediator.Send(
            //     new GetRecentAttachmentsQuery(options.Limit), 
            //     cancellationToken);

            foreach (AttachmentReadDto? attachment in await attachmentsService.GetListAsync(new AttachmentDescriptorDto { Take = 5 }, cancellationToken).GetValueOrDefaultAsync() ?? [])
            {
                feedItems.Add(CreateFeedItem(attachment));
            }

            return feedItems;
        }
        catch
        {
            return [];
        }
    }

    private FeedItem CreateFeedItem(AttachmentReadDto attachment)
    {
        string fileName = System.IO.Path.GetFileName(attachment.Content) ?? "Unnamed File";

        return new FeedItem
        {
            Id = attachment.Id,
            SourceId = SourceId,
            SourceName = SourceName,
            Title = fileName,
            Timestamp = attachment.UpdatedAt ?? attachment.CreatedAt,
            ItemType = "attachment",
            IconUrl = attachment.ContentThumbnail,
            NavigationUrl = $"/sea/events/{attachment.EventId}",
            CreatedBy = attachment.CreatedBy,
            Metadata = new Dictionary<string, object?>
            {
                ["contentType"] = attachment.ContentType,
                ["size"] = attachment.Size,
                ["eventId"] = attachment.EventId,
                ["content"] = attachment.Content
            }
        };
    }
}
