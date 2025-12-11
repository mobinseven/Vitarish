using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Vitarish.Clients.Gateway.Abstractions;
using Vitarish.Clients.Gateway.Constants;
using Vitarish.Clients.Gateway.Options;

namespace Vitarish.Clients.Gateway.Internal;

/// <summary>
/// Client-side implementation of <see cref="IClientIdentityContext"/> that reads identity
/// information from <see cref="IdentityOptions"/> and the current user's claims.
/// </summary>
/// <remarks>
/// The claim type constants (TenantIdClaim, OwnerIdClaim) are shared with the server-side
/// implementation via the identity provider contract. Changes to these claim names must be
/// coordinated with the server-side <c>RequestIdentityConstants</c> in the RequestIdentities module.
/// </remarks>
internal sealed class ClientIdentityContext : IClientIdentityContext
{
    private readonly IdentityOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ClientIdentityContext(IOptions<IdentityOptions> options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string Authority
    {
        get
        {
            if (_options.Authority is null)
            {
                throw new InvalidOperationException("AAAS Authority is not configured. Please ensure 'Aaas.Authority' is set in the configuration.");
            }
            return _options.Authority;
        }
    }

    /// <inheritdoc />
    public string ClientId
    {
        get
        {
            if (_options.ClientId is null)
            {
                throw new InvalidOperationException("ClientId is not configured. Please ensure that IdentityOptions.ClientId is set in the configuration.");
            }
            return _options.ClientId;
        }
    }

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
            if (string.IsNullOrEmpty(ClientId) && string.IsNullOrEmpty(ownerIdValue))
            {
                return null;
            }
            
            return string.IsNullOrEmpty(ClientId) ? ownerIdValue : $"{ClientId}.{ownerIdValue}";
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
            if (headers is not null && headers.TryGetValue("X-Request-Id", out Microsoft.Extensions.Primitives.StringValues value))
            {
                return value.FirstOrDefault();
            }
            return null;
        }
    }

    private ClaimsPrincipal? User => _httpContextAccessor?.HttpContext?.User;
}
