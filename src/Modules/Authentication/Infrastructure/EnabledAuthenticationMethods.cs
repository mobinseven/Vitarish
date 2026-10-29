using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure;

internal sealed class EnabledAuthenticationMethodsReader(IAuthenticationSecurityPolicyProvider policyProvider)
    : IEnabledAuthenticationMethods
{
    public async Task<EnabledAuthenticationMethods> GetAsync(CancellationToken cancellationToken)
    {
        SecurityAccessPolicy policy = await policyProvider.GetAsync(cancellationToken);
        return new(
            policy.EnabledAuthenticationMechanisms.Contains(AuthenticationMethod.Password),
            policy.EnabledAuthenticationMechanisms.Contains(AuthenticationMethod.Phone),
            policy.EnabledAuthenticationMechanisms.Contains(AuthenticationMethod.Email),
            policy.EnabledAuthenticationMechanisms.Contains(AuthenticationMethod.Passkey));
    }
}
