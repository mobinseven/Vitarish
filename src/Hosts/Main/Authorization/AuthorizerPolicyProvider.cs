using Microsoft.AspNetCore.Authorization;

namespace Vitarish.Main.Authorization;

public class AuthorizerPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        AuthorizationPolicy? registeredPolicy = await _fallback.GetPolicyAsync(policyName);
        if (registeredPolicy is not null)
        {
            return registeredPolicy;
        }

        AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new AuthorizerRequirement(policyName))
            .Build();
        return policy;
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();
}
