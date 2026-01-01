using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.Models;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Main.Infrastructure.IdentityServer;

/// <summary>
/// Custom profile service that adds party claims to tokens
/// for user authentication flows.
/// </summary>
public sealed class PartyProfileService(
    UserManager<User> userManager,
    IUserClaimsPrincipalFactory<User> claimsFactory,
    IHttpContextAccessor httpContextAccessor,
    ITenantsService tenantsService,
    ILogger<PartyProfileService> logger)
    : ProfileService<User>(userManager, claimsFactory, logger)
{
    public override async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        await base.GetProfileDataAsync(context);

        await AddPartyClaimsAsync(context);
    }

    private async Task AddPartyClaimsAsync(ProfileDataRequestContext context)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            logger.LogDebug("HttpContext is null, skipping tenant/owner claim resolution");
            return;
        }

        try
        {
            Tenant? tenant = await tenantsService.ResolveFromHostAsync(httpContext, CancellationToken.None);
            if (tenant is not null)
            {
                context.IssuedClaims.Add(new Claim(IdentityDefinitions.ClientIdClaim, tenant.ClientId));
                context.IssuedClaims.Add(new Claim(IdentityDefinitions.OwnerIdClaim, tenant.OwnerId.ToString()));
                context.IssuedClaims.Add(new Claim(IdentityDefinitions.TenantIdClaim, tenant.Id.ToString()));

                logger.LogDebug(
                    "Added tenant/owner claims for user {Subject}: TenantId={TenantId}, OwnerId={OwnerId}",
                    context.Subject?.Identity?.Name,
                    tenant.Id,
                    tenant.OwnerId);
            }
            else
            {
                logger.LogDebug(
                    "No tenant resolved from host for user {Subject}, tenant/owner claims not added",
                    context.Subject?.Identity?.Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve tenant for user {Subject}", context.Subject?.Identity?.Name);
        }
    }
}
