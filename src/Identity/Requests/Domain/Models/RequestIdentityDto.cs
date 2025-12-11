namespace Vitarish.Requests.Domain.Models;

public sealed record RequestIdentityDto(
    string RequestId,
    string Authority,
    string ClientId,
    string OwnerId,
    string? TenantId,
    string? UserId,
    string? ResourceId,
    string? TraceId,
    string? AdditionalInfo);
