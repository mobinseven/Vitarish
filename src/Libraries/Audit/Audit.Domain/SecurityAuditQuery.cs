namespace Vitarish.Audit.Domain;

public enum SecurityAuditSortField
{
    Actor,
    OccurredAt,
    EventType
}

public sealed record SecurityAuditQuery(
    int Page = 1,
    int PageSize = 50,
    string? ActorId = null,
    string? EventName = null,
    string? EventCategory = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    SecurityAuditSortField SortBy = SecurityAuditSortField.OccurredAt,
    bool Descending = true);

public sealed record SecurityAuditPageReadDto(
    IReadOnlyList<SecurityAuditEventReadDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool CapacityThresholdReached);
