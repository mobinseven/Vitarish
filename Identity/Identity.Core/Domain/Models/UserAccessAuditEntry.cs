namespace Vitarish.Identity.Core.Domain.Models;

/// <summary>
/// Audit entry for user access changes.
/// </summary>
public sealed record UserAccessAuditEntry(
    DateTimeOffset Timestamp,
    string? AdminUserId,
    string? OwnerId,
    string TargetUserId,
    string TargetClientId,
    string Action);
