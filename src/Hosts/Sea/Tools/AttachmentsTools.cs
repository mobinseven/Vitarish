

namespace Vitarish.Sea.Tools;

[McpServerToolType]
public static class AttachmentsTools
{
    [McpServerTool(Name = nameof(GetAttachmentsAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query retrieves attachments for the specified event.")]
    public static Task<IReadOnlyList<AttachmentDto>> GetAttachmentsAsync(
                IAttachmentsClient AttachmentsService,
                Guid eventId,
                CancellationToken cancellationToken = default) => AttachmentsService.GetByEventAsync(eventId, cancellationToken);

    [McpServerTool(Name = nameof(GetAttachmentsCountAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = false), Description("This query gets the number of attachments for the specified event.")]
    public static Task<int> GetAttachmentsCountAsync(
                IAttachmentsClient AttachmentsService,
                Guid eventId,
                CancellationToken cancellationToken = default) => AttachmentsService.GetCountByEventAsync(eventId, cancellationToken);

    [McpServerTool(Name = nameof(AddAttachmentAsync), Destructive = false, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = true), Description("This command adds a new attachment to an event.")]
    public static Task<AttachmentDto?> AddAttachmentAsync(
                IAttachmentsClient AttachmentsService,
                Guid eventId,
                string content,
                string contentType,
                string? contentThumbnail,
                long size,
                CancellationToken cancellationToken = default) => AttachmentsService.AddAsync(eventId, content, contentType, contentThumbnail, size, cancellationToken);

    [McpServerTool(Name = nameof(AddAttachmentsAsync), Destructive = false, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command adds multiple attachments to an event.")]
    public static Task AddAttachmentsAsync(
                IAttachmentsClient AttachmentsService,
                Guid eventId,
                IEnumerable<AttachmentDto> attachments,
                CancellationToken cancellationToken = default) => AttachmentsService.AddAsync(eventId, attachments, cancellationToken);

    [McpServerTool(Name = nameof(RemoveAttachmentAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command removes an attachment from an event.")]
    public static Task RemoveAttachmentAsync(
                IAttachmentsClient AttachmentsService,
                Guid eventId,
                Guid attachmentId,
                CancellationToken cancellationToken = default) => AttachmentsService.RemoveAsync(eventId, attachmentId, cancellationToken);

    [McpServerTool(Name = nameof(UpdateAttachmentAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = true), Description("This command updates the content of an existing attachment.")]
    public static Task<AttachmentDto?> UpdateAttachmentAsync(
                IAttachmentsClient AttachmentsService,
                Guid eventId,
                Guid attachmentId,
                string content,
                CancellationToken cancellationToken = default) => AttachmentsService.UpdateAsync(eventId, attachmentId, content, cancellationToken);

    [McpServerTool(Name = nameof(GetAttachmentSnapshotAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query retrieves a snapshot for the specified attachment.")]
    public static Task<AttachmentSnapshot?> GetAttachmentSnapshotAsync(
                IAttachmentsClient AttachmentsService,
                Guid id,
                CancellationToken cancellationToken = default) => AttachmentsService.CaptureSnapshotAsync(id, cancellationToken);
}

