using Vitarish.Contents.Application.Models;

namespace Vitarish.Contents.Infrastructure.Data;

internal sealed class ContentPublicationOutboxRecord
{
    internal const string TableName = "ContentPublicationOutbox";

    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid ContentId { get; set; }
    public Guid? RevisionId { get; set; }
    public ContentPublicationOperation Operation { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}
