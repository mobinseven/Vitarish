using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Main.Infrastructure.IdentityServer;

/// <summary>
/// Custom token request validator that adds tenant and owner claims to API client tokens.
/// Similar to how <see cref="PartyProfileService"/> adds claims for user authentication flows,
/// this service resolves tenant/Owner information from the HTTP request context for client credentials flow.
/// </summary>
public sealed class ApiClientTokenValidator(
    IHttpContextAccessor httpContextAccessor,
    ITenantResolver tenantsService,
    ILogger<ApiClientTokenValidator> logger)
    : ICustomTokenRequestValidator
{
    public async Task ValidateAsync(CustomTokenRequestValidationContext context)
    {
        // Only process client credentials requests
        if (context.Result?.ValidatedRequest is not { GrantType: GrantType.ClientCredentials } validatedRequest)
        {
            return;
        }

        await AddTenantOwnerClaimsAsync(validatedRequest);
    }

    private async Task AddTenantOwnerClaimsAsync(ValidatedTokenRequest request)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            logger.LogDebug("HttpContext is null, skipping tenant/Owner claim resolution for client {ClientId}",
                request.Client?.ClientId);
            return;
        }

        try
        {
            Tenant? tenant = await tenantsService.ResolveAsync(httpContext, httpContext.RequestAborted);
            if (tenant is not null)
            {
                request.ClientClaims.Add(new Claim(IdentityDefinitions.ClientIdClaim, tenant.ClientId));
                request.ClientClaims.Add(new Claim(IdentityDefinitions.OwnerIdClaim, tenant.OwnerId.ToString()));
                request.ClientClaims.Add(new Claim(IdentityDefinitions.TenantIdClaim, tenant.Id.ToString()));

                logger.LogDebug(
                    "Added tenant/Owner claims for API client {ClientId}: TenantId={TenantId}, OwnerId={OwnerId}",
                    request.Client?.ClientId,
                    tenant.Id,
                    tenant.OwnerId);
            }
            else
            {
                logger.LogDebug(
                    "No tenant resolved from host for API client {ClientId}, tenant/Owner claims not added",
                    request.Client?.ClientId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve tenant for API client {ClientId}",
                request.Client?.ClientId);
        }
    }
}
