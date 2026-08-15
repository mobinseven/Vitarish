using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Main.Infrastructure.IdentityServer;

public sealed class ApiClientTokenValidator(
    IHttpContextAccessor httpContextAccessor,
    ITenantResolver tenantsService,
    ILogger<ApiClientTokenValidator> logger)
    : ICustomTokenRequestValidator
{
    public async Task ValidateAsync(CustomTokenRequestValidationContext context)
    {
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
            logger.LogDebug("HttpContext is null, skipping tenant/Owner claim resolution for client {AppId}", request.Client?.ClientId);
            return;
        }

        try
        {
            Tenant? tenant = await tenantsService.ResolveAsync(httpContext, httpContext.RequestAborted);
            if (tenant is not null)
            {
                request.ClientClaims.Add(new Claim(IdentityDefinitions.ClientIdClaim, tenant.AppId));
                request.ClientClaims.Add(new Claim(IdentityDefinitions.OwnerIdClaim, tenant.OwnerId.ToString()));
                request.ClientClaims.Add(new Claim(IdentityDefinitions.TenantIdClaim, tenant.Id.ToString()));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve tenant for API client {AppId}", request.Client?.ClientId);
        }
    }
}
