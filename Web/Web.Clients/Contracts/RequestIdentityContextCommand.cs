namespace Vitarish.Web.Clients.Contracts;

/// <summary>
/// Command to write request identity context.
/// </summary>
public sealed record RequestIdentityContextCommand(
    string Authority,
    string ClientId,
    string OwnerId,
    string? TenantId,
    string? UserId,
    string? ResourceId,
    string? TraceId,
    string? RequestId,
    string? AdditionalInfo);
