namespace Vorn.Aaas.Client.Api.Models;

internal sealed record RequestIdentityDto(
    string RequestId,
    string Authority,
    string ClientId,
    string OwnerId,
    string? TenantId,
    string? UserId,
    string? ResourceId,
    string? TraceId,
    string? AdditionalInfo,
    DateTimeOffset CreatedAt);
