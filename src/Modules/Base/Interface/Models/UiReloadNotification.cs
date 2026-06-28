namespace Vitarish.Modules.Base.Interface.Models;

public sealed class UiReloadNotification(string? subject, string? subjectId, string? tenantId) : EventArgs
{
    public string? Subject { get; } = subject;
    public string? SubjectId { get; } = subjectId;
    public string? TenantId { get; } = tenantId;
}
