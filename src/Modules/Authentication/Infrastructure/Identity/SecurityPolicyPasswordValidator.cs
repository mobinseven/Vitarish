using Microsoft.AspNetCore.Identity;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

public sealed class SecurityPolicyPasswordValidator(IAuthenticationSecurityPolicyProvider policyProvider) : IPasswordValidator<User>
{
    public async Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string? password)
    {
        SecurityAccessPolicy policy = await policyProvider.GetAsync(CancellationToken.None);
        IReadOnlyList<PasswordPolicyViolation> violations = policy.PasswordPolicy.Validate(password);
        return violations.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed(violations.Select(violation => new IdentityError
            {
                Code = $"PasswordPolicy.{violation}",
                Description = $"Password does not satisfy the {violation} requirement."
            }).ToArray());
    }
}
