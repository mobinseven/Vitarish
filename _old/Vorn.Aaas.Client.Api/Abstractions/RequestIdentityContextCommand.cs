namespace Vorn.Aaas.Client.Api.Abstractions;

public readonly record struct RequestIdentityContextCommand(
    string Authority,
    string ClientId,
    string OwnerId,
    string? TenantId,
    string? UserId,
    string? ResourceId,
    string? TraceId,
    string RequestId,
    string? AdditionalInfo);
