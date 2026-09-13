using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Sea.Application.Abstractions;

namespace Vitarish.Sea.Application.Access;

/// <summary>
/// Binds SEA resource authorization to the trusted tenant and authenticated subject.
/// </summary>
public sealed class SeaResourceAccess(
    ISeaManagementAccessEvaluator accessEvaluator,
    IRequestContext requestContext) : ISeaResourceAccess
{
    public Task<bool> CanManageAsync(CancellationToken cancellationToken = default) =>
        WithSubjectAsync((subject, ct) => accessEvaluator.CanManageAsync(subject, ct), cancellationToken);

    public Task<bool> CanReadProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        WithSubjectAsync((subject, ct) => accessEvaluator.CanReadProjectAsync(subject, projectId, ct), cancellationToken);

    public Task<bool> CanReadProcedureAsync(Guid procedureId, CancellationToken cancellationToken = default) =>
        WithSubjectAsync((subject, ct) => accessEvaluator.CanReadProcedureAsync(subject, procedureId, ct), cancellationToken);

    public Task<bool> CanReadEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        WithSubjectAsync((subject, ct) => accessEvaluator.CanReadEventAsync(subject, eventId, ct), cancellationToken);

    public Task<bool> CanWriteEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        WithSubjectAsync((subject, ct) => accessEvaluator.CanWriteEventAsync(subject, eventId, ct), cancellationToken);

    public Task<bool> CanReadAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default) =>
        WithSubjectAsync((subject, ct) => accessEvaluator.CanReadAttachmentAsync(subject, attachmentId, ct), cancellationToken);

    public Task<bool> CanWriteAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default) =>
        WithSubjectAsync((subject, ct) => accessEvaluator.CanWriteAttachmentAsync(subject, attachmentId, ct), cancellationToken);

    private Task<bool> WithSubjectAsync(
        Func<SeaManagementAgentSubject, CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(requestContext.TenantId) || string.IsNullOrWhiteSpace(requestContext.UserId))
        {
            return Task.FromResult(false);
        }

        return operation(new SeaManagementAgentSubject(requestContext.TenantId, requestContext.UserId), cancellationToken);
    }
}
