using MediatR;
using Vitarish.Modules.Feed.Domain.Abstractions;
using Vitarish.Modules.Feed.Domain.Models;
using Vitarish.Sea.Application.Queries.Events;
using Vitarish.Sea.Domain.Entities;

namespace Vitarish.Sea.Infrastructure.FeedSources;

/// <summary>
/// Feed data source that provides recent attachments from the Sea estate
/// </summary>
public class SeaAttachmentsFeedSource : IFeedDataSource
{
    private readonly IMediator _mediator;

    public SeaAttachmentsFeedSource(IMediator mediator)
    {
        _mediator = mediator;
    }

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
            var feedItems = new List<FeedItem>();

            // For now, return empty list as placeholder
            // A proper implementation would query the database for recent attachments
            // Example:
            // var recentAttachments = await _mediator.Send(
            //     new GetRecentAttachmentsQuery(options.Limit), 
            //     cancellationToken);
            
            // foreach (var attachment in recentAttachments)
            // {
            //     feedItems.Add(CreateFeedItem(attachment));
            // }

            return feedItems;
        }
        catch
        {
            return [];
        }
    }

    private FeedItem CreateFeedItem(Attachment attachment)
    {
        // Extract filename from Content property (which is a file path or URL)
        var fileName = System.IO.Path.GetFileName(attachment.Content) ?? "Unnamed File";
        
        return new FeedItem
        {
            Id = attachment.Id,
            SourceId = SourceId,
            SourceName = SourceName,
            Title = fileName,
            Description = $"{FormatFileSize(attachment.Size)} • {attachment.ContentType}",
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

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
