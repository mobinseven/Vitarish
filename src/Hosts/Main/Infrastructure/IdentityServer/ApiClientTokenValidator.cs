using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Vitarish.Main.Services;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Main.Infrastructure.IdentityServer;

/// <summary>
/// Custom token request validator that adds tenant and owner claims to API client tokens.
/// Similar to how <see cref="PartyProfileService"/> adds claims for user authentication flows,
/// this service resolves tenant/owner information from the HTTP request context for client credentials flow.
/// </summary>
public sealed class ApiClientTokenValidator(
    IHttpContextAccessor httpContextAccessor,
    ITenantsService tenantsService,
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
            logger.LogDebug("HttpContext is null, skipping tenant/owner claim resolution for client {ClientId}",
                request.Client?.ClientId);
            return;
        }

        try
        {
            Tenant? tenant = await tenantsService.ResolveFromHostAsync(httpContext, httpContext.RequestAborted);
            if (tenant is not null)
            {
                request.ClientClaims.Add(new Claim(IdentityDefinitions.TenantIdClaim, tenant.Id.ToString()));
                request.ClientClaims.Add(new Claim(IdentityDefinitions.OwnerIdClaim, tenant.OwnerId.ToString()));

                logger.LogDebug(
                    "Added tenant/owner claims for API client {ClientId}: TenantId={TenantId}, OwnerId={OwnerId}",
                    request.Client?.ClientId,
                    tenant.Id,
                    tenant.OwnerId);
            }
            else
            {
                logger.LogDebug(
                    "No tenant resolved from host for API client {ClientId}, tenant/owner claims not added",
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
