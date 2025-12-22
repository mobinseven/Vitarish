using Vitarish.Main.Infrastructure.IdentityServer;

namespace Vitarish.Main.Services;

public interface ITenantResolver
{
    string? ResolveTenantId(HttpContext httpContext);
}

public interface ITenantRoleStore
{
    Task<IReadOnlyList<string>> GetRolesAsync(string userId, string tenantId);
}

public sealed class TenantRoleClaimsTransformation(
        IHttpContextAccessor http,
        ITenantResolver tenantResolver,
        ITenantRoleStore roleStore) : IClaimsTransformation
{

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        HttpContext? ctx = http.HttpContext;
        if (ctx is null)
        {
            return principal;
        }

        string? tenantId = tenantResolver.ResolveTenantId(ctx);
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return principal;
        }

        string? userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return principal;
        }

        IReadOnlyList<string> roles = await roleStore.GetRolesAsync(userId, tenantId);

        // Rebuild principal so we don't accumulate tenant-role identities across requests.
        List<ClaimsIdentity> identities = [.. principal.Identities.Where(i => i.AuthenticationType != IdentityDefinitions.TenantRolesAuthType)];

        ClaimsIdentity tenantIdentity = new(IdentityDefinitions.TenantRolesAuthType);
        tenantIdentity.AddClaim(new Claim(IdentityDefinitions.TenantIdClaim, tenantId));

        foreach (string role in roles)
        {
            tenantIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        identities.Add(tenantIdentity);
        return new ClaimsPrincipal(identities);
    }
}
