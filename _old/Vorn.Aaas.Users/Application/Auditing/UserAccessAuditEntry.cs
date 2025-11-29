namespace Vorn.Aaas.Users.Application.Auditing;

public sealed record UserAccessAuditEntry(
    DateTimeOffset Timestamp,
    string? AdminUserId,
    string? OwnerId,
    string TargetUserId,
    string TargetClientId,
    string Action);
