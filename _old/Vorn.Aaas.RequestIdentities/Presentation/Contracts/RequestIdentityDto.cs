using Vorn.Aaas.RequestIdentities.Application.Commands;

namespace Vorn.Aaas.RequestIdentities.Presentation.Contracts;

public sealed record RequestIdentityDto(
    string RequestId,
    string Authority,
    string ClientId,
    string OwnerId,
    string? TenantId,
    string? UserId,
    string? ResourceId,
    string? TraceId,
    string? AdditionalInfo)
{
    public RecordRequestIdentityCommand ToCommand() => new(
        RequestId,
        Authority,
        ClientId,
        OwnerId,
        TenantId,
        UserId,
        ResourceId,
        TraceId,
        AdditionalInfo);
}
