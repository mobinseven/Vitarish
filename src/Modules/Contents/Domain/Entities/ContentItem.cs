using System.Text.Json;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Entities;

public class ContentItem : Entity
{
    public ContentItemType Type { get; set; } = ContentItemType.Article;
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public string Slug { get; set; } = string.Empty;
    public string CanonicalPath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public IReadOnlyList<ContentBodyBlock> Body { get; set; } = [];
    public Dictionary<string, object?> Metadata { get; set; } = [];
    public Guid AuthorId { get; set; }
    public string Locale { get; set; } = "en";
    public DateTime? PublishedAt { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? ContentTypeId { get; set; }
    public int? SchemaVersion { get; set; }
    public JsonElement? Fields { get; set; }
    public Guid? WorkingRevisionId { get; set; }
    public Guid? PublishedRevisionId { get; set; }
    public int? Version { get; set; }

    public void SaveDraft(ContentRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);
        EnsureRevisionBelongsToItem(revision);

        WorkingRevisionId = revision.Id;
        Fields = revision.Fields?.Clone();
        SchemaVersion = revision.SchemaVersion;
        Slug = revision.Slug ?? Slug;
        CanonicalPath = revision.CanonicalPath ?? CanonicalPath;
        Title = revision.Title ?? Title;
        Excerpt = revision.Excerpt ?? Excerpt;
        SynchronizeStatus();
    }

    public void Publish(ContentRevision revision, DateTime publishedAt)
    {
        ArgumentNullException.ThrowIfNull(revision);
        EnsureRevisionBelongsToItem(revision);

        if (Status == ContentStatus.Archived)
        {
            throw new EntityFailureException(EntityError.Validation("Archived content cannot be published.", nameof(Status)));
        }

        PublishedRevisionId = revision.Id;
        PublishedAt = publishedAt;
        SynchronizeStatus();
    }

    public void Unpublish()
    {
        PublishedRevisionId = null;
        PublishedAt = null;
        SynchronizeStatus();
    }

    public void Archive()
    {
        Status = ContentStatus.Archived;
    }

    private void EnsureRevisionBelongsToItem(ContentRevision revision)
    {
        if (revision.Id == Guid.Empty || revision.ContentId != Id)
        {
            throw new EntityFailureException(EntityError.Validation("Revision must belong to this content item.", nameof(revision)));
        }
    }

    private void SynchronizeStatus()
    {
        Status = Status == ContentStatus.Archived
            ? ContentStatus.Archived
            : PublishedRevisionId is not null
                ? ContentStatus.Published
                : ContentStatus.Draft;
    }
}
