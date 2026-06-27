using System.Security.Claims;
using Vitarish.Requests.Contracts;

namespace Vitarish.Requests.Client;

public sealed class ClaimsPrincipalRequestContextResolver : IRequestContextResolver
{
    private static readonly string[] KnownClaimTypes =
    [
        RequestContextClaimTypes.RequestId,
        RequestContextClaimTypes.Authority,
        RequestContextClaimTypes.ClientId,
        RequestContextClaimTypes.OwnerId,
        RequestContextClaimTypes.TenantId,
        RequestContextClaimTypes.UserId,
        RequestContextClaimTypes.ResourceId,
        RequestContextClaimTypes.TraceId
    ];

    public RequestContextResult Resolve(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity?.IsAuthenticated != true)
        {
            return Failed(RequestContextFailureKind.Unauthenticated, "The current principal is not authenticated.");
        }

        string? clientId = Read(principal, RequestContextClaimTypes.ClientId);
        string? ownerId = Read(principal, RequestContextClaimTypes.OwnerId);
        string? tenantId = Read(principal, RequestContextClaimTypes.TenantId);
        string? userId = Read(principal, RequestContextClaimTypes.UserId) ?? Read(principal, ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(clientId) && string.IsNullOrWhiteSpace(userId))
        {
            return Failed(RequestContextFailureKind.MissingRequiredClaim, "Either client or user claim is required.");
        }

        if (!IsGuidLike(ownerId) || !IsGuidLike(tenantId) || !IsGuidLike(userId))
        {
            return Failed(RequestContextFailureKind.InvalidClaimValue, "One or more identifier claims are not valid GUID values.");
        }

        RequestContextDto context = new(
            Read(principal, RequestContextClaimTypes.RequestId),
            Read(principal, RequestContextClaimTypes.Authority),
            clientId,
            ownerId,
            tenantId,
            userId,
            Read(principal, RequestContextClaimTypes.ResourceId),
            Read(principal, RequestContextClaimTypes.TraceId),
            principal.Claims
                .Where(claim => !KnownClaimTypes.Contains(claim.Type, StringComparer.Ordinal) && claim.Type != ClaimTypes.NameIdentifier)
                .GroupBy(claim => claim.Type, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal));

        return new RequestContextResult(context, null);
    }

    private static string? Read(ClaimsPrincipal principal, string claimType) => principal.FindFirst(claimType)?.Value;
    private static bool IsGuidLike(string? value) => string.IsNullOrWhiteSpace(value) || Guid.TryParse(value, out _);
    private static RequestContextResult Failed(RequestContextFailureKind kind, string message) => new(null, new RequestContextFailure(kind, message));
}
