using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Vitarish.Clients.Interface.Identity.Services;

public sealed class TenantRolePolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    public const string Prefix = "TenantRole:";
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            string role = policyName[Prefix.Length..];
            AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new TenantRoleRequirement(role))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();
}
