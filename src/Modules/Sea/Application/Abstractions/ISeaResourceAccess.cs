namespace Vitarish.Sea.Application.Abstractions;

/// <summary>
/// Application-owned authorization boundary for SEA user-data resources.
/// </summary>
public interface ISeaResourceAccess
{
    Task<bool> CanManageAsync(CancellationToken cancellationToken = default);
    Task<bool> CanReadProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> CanReadProcedureAsync(Guid procedureId, CancellationToken cancellationToken = default);
    Task<bool> CanReadEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> CanWriteEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> CanReadAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default);
    Task<bool> CanWriteAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default);
}
