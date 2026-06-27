namespace Vitarish.Requests.Contracts;

public sealed record RequestContextDto(
    string? RequestId,
    string? Authority,
    string? ClientId,
    string? OwnerId,
    string? TenantId,
    string? UserId,
    string? ResourceId,
    string? TraceId,
    IReadOnlyDictionary<string, string> AdditionalClaims);

public sealed record RequestContextResult(RequestContextDto? Context, RequestContextFailure? Failure);

public sealed record RequestContextFailure(RequestContextFailureKind Kind, string Message);

public enum RequestContextFailureKind
{
    Unauthenticated,
    MissingRequiredClaim,
    InvalidClaimValue
}
