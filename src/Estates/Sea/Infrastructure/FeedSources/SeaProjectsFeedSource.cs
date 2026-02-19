using MediatR;
using Vitarish.Modules.Feed.Domain.Abstractions;
using Vitarish.Modules.Feed.Domain.Models;
using Vitarish.Sea.Domain.Entities;

namespace Vitarish.Sea.Infrastructure.FeedSources;

/// <summary>
/// Feed data source that provides recent projects from the Sea estate
/// </summary>
public class SeaProjectsFeedSource : IFeedDataSource
{
    private readonly IMediator _mediator;

    public SeaProjectsFeedSource(IMediator mediator)
    {
        _mediator = mediator;
    }

    public string SourceId => "sea.projects";
    public string SourceName => "Sea Projects";
    public string Description => "Recently created or updated Sea projects";
    public int Priority => 20;
    public bool EnabledByDefault => true;

    public async Task<IReadOnlyList<FeedItem>> GetFeedItemsAsync(
        FeedQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Query recent projects
            // Note: This is a simplified example. In a real implementation,
            // you'd want a dedicated query for recent projects
            var feedItems = new List<FeedItem>();

            // For now, return empty list as placeholder
            // A proper implementation would query the database for recent projects
            // Example:
            // var recentProjects = await _mediator.Send(
            //     new GetRecentProjectsQuery(options.Limit), 
            //     cancellationToken);
            
            // foreach (var project in recentProjects)
            // {
            //     feedItems.Add(CreateFeedItem(project));
            // }

            return feedItems;
        }
        catch
        {
            return [];
        }
    }

    private FeedItem CreateFeedItem(Project project)
    {
        return new FeedItem
        {
            Id = project.Id,
            SourceId = SourceId,
            SourceName = SourceName,
            Title = project.Name,
            Description = project.Description,
            Timestamp = project.UpdatedAt ?? project.CreatedAt,
            ItemType = "project",
            IconUrl = null, // Could extract from project metadata
            NavigationUrl = $"/sea/projects/{project.Id}",
            CreatedBy = project.CreatedBy,
            Metadata = new Dictionary<string, object?>
            {
                ["isTemplate"] = project.Template,
                ["metadata"] = project.Metadata
            }
        };
    }
}
