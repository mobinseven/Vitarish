namespace Vorn.Aaas.Client.Api.Abstractions;

public interface IRequestIdentityContext
{
    string Authority { get; }
    string ClientId { get; }
    string OwnerId { get; }
    string? UserId { get; }
    string? TenantId { get; }
    string? ResourceId { get; }
    string? TraceId { get; }
    string? RequestId { get; }
    string? AdditionalInfo { get; }
}
