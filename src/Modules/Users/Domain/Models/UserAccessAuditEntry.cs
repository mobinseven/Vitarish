namespace Vitarish.Users.Domain.Models;

public sealed record UserAccessAuditEntry(
    DateTime Timestamp,
    string? AdminUserId,
    string? OwnerId,
    string TargetUserId,
    string TargetClientId,
    string Action);
