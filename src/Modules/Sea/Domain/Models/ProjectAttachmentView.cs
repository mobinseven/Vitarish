namespace Vitarish.Sea.Domain.Models;

public sealed class ProjectAttachmentView
{
    public Guid AttachmentId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid ProcedureId { get; init; }
    public Guid EventId { get; init; }
    public string Content { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string? ContentThumbnail { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
}
