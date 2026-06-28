namespace Vitarish.Modules.Base.Interface.Models;

public sealed record UiReloadNotification(string? Subject, string? SubjectId, string? TenantId) : EventArgs;
