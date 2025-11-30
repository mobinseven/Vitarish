namespace Vorn.Aaas.Users.Domain.Models;

public sealed record UserAccessAuditEntry(
    DateTimeOffset Timestamp,
    string? AdminUserId,
    string? OwnerId,
    string TargetUserId,
    string TargetClientId,
    string Action);
