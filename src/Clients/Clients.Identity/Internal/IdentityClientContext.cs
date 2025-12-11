using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Constants;
using Vitarish.Clients.Identity.Options;

namespace Vitarish.Clients.Identity.Internal;

/// <summary>
/// Client-side implementation of <see cref="IIdentityClientContext"/> that reads identity
/// information from <see cref="IdentityClientOptions"/> and the current user's claims.
/// </summary>
/// <remarks>
/// The claim type constants (TenantIdClaim, OwnerIdClaim) are shared with the server-side
/// implementation via the identity provider contract. Changes to these claim names must be
/// coordinated with the server-side <c>RequestIdentityConstants</c> in the RequestIdentities module.
/// </remarks>
internal sealed class IdentityClientContext : IIdentityClientContext
{
    private readonly IdentityClientOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public IdentityClientContext(IOptions<IdentityClientOptions> options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string Authority => _options.Authority is null
                ? throw new InvalidOperationException("AAAS Authority is not configured. Please ensure 'Aaas.Authority' is set in the configuration.")
                : _options.Authority;

    /// <inheritdoc />
    public string ClientId => _options.ClientId is null
                ? throw new InvalidOperationException("ClientId is not configured. Please ensure that IdentityClientOptions.ClientId is set in the configuration.")
                : _options.ClientId;

    /// <inheritdoc />
    public string OwnerId
    {
        get
        {
            string? claimOwnerId = User?.FindFirstValue(AaasConstants.OwnerIdClaim);
            return claimOwnerId ?? ClientId;
        }
    }

    /// <inheritdoc />
    public string? UserId => User?.FindFirstValue("sub");

    /// <inheritdoc />
    public string? TenantId
    {
        get
        {
            string? claimTenantId = User?.FindFirstValue(AaasConstants.TenantIdClaim);
            if (!string.IsNullOrEmpty(claimTenantId))
            {
                return claimTenantId;
            }

            // If no tenant ID claim is present, synthesize it from ClientId and OwnerId.
            // Returns null if both ClientId and OwnerId are empty.
            string ownerIdValue = OwnerId;
            return string.IsNullOrEmpty(ClientId) && string.IsNullOrEmpty(ownerIdValue)
                ? null
                : string.IsNullOrEmpty(ClientId) ? ownerIdValue : $"{ClientId}.{ownerIdValue}";
        }
    }

    /// <inheritdoc />
    public string? ResourceId => _httpContextAccessor?.HttpContext?.Request.Path.ToString();

    /// <inheritdoc />
    public string? RequestId
    {
        get
        {
            IHeaderDictionary? headers = _httpContextAccessor?.HttpContext?.Request.Headers;
            return headers is not null && headers.TryGetValue("X-Request-Id", out Microsoft.Extensions.Primitives.StringValues value)
                ? value.FirstOrDefault()
                : null;
        }
    }

    private ClaimsPrincipal? User => _httpContextAccessor?.HttpContext?.User;
}
