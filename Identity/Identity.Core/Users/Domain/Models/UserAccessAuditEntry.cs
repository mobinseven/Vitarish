namespace Vitarish.Identity.Core.Users.Domain.Models;

/// <summary>
/// Represents an audit entry for user access changes.
/// </summary>
public sealed record UserAccessAuditEntry(
    DateTimeOffset Timestamp,
    string? AdminUserId,
    string? OwnerId,
    string TargetUserId,
    string TargetClientId,
    string Action);
