namespace Vorn.Aaas.Client.Api.Contracts;

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
