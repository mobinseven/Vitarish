namespace Vorn.Aaas.Client.Api.Contracts;

public sealed record ScopeRequestIdentity(
    string? OwnerId = null,
    string? TenantId = null,
    string? ResourceId = null,
    string? RequestId = null,
    string? AdditionalInfo = null);
