namespace Vitarish.Requests.Contracts;

public sealed record RequestIdentityQueryParameters
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Search { get; init; }
    public string? RequestId { get; init; }
    public string? Authority { get; init; }
    public string? ClientId { get; init; }
    public string? OwnerId { get; init; }
    public string? TenantId { get; init; }
    public string? UserId { get; init; }
    public string? ResourceId { get; init; }
    public string? TraceId { get; init; }
    public string? AdditionalInfo { get; init; }
}

public sealed record RequestIdentitySortParameter(string Field, bool Descending);

public sealed record RequestIdentityResponse(
    Guid Id,
    string RequestId,
    string Authority,
    string? ClientId,
    string? OwnerId,
    string? TenantId,
    string? UserId,
    string? ResourceId,
    string? TraceId,
    string? AdditionalInfo,
    DateTime CreatedAt);

public sealed record RequestIdentityPageResponse(
    IReadOnlyList<RequestIdentityResponse> Items,
    int TotalItems,
    int Page,
    int PageSize);

public sealed record RequestsErrorResponse(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
