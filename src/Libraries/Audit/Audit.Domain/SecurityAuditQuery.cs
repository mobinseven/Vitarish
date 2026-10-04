namespace Vitarish.Audit.Domain;

public enum SecurityAuditSortField
{
    Actor,
    OccurredAt,
    EventType,
    ActorKind,
    SourceIpKind,
    SourceIpAddress
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
    bool Descending = true,
    string? Operation = null,
    string? Outcome = null,
    string? EventId = null,
    string? SubjectId = null);

public sealed record SecurityAuditPlatformQuery(
    SecurityAuditQuery Query,
    string? TenantId = null,
    bool UncataloguedOnly = false);

public sealed record SecurityAuditCatalogEntryReadDto(
    string Category,
    string EventName,
    string Operation,
    bool IsWildcard);

public sealed record SecurityAuditPageReadDto(
    IReadOnlyList<SecurityAuditEventReadDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool CapacityThresholdReached);
