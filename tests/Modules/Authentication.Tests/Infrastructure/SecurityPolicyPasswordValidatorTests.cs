using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class SecurityPolicyPasswordValidatorTests
{
    [Fact]
    public async Task Rejects_passwords_that_miss_any_required_complexity_category()
    {
        Mock<IAuthenticationSecurityPolicyProvider> provider = new();
        provider.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        SecurityPolicyPasswordValidator validator = new(provider.Object);

        IdentityResult result = await validator.ValidateAsync(null!, new User(), "abcdefgh");

        result.Succeeded.Should().BeFalse();
        result.Errors.Select(error => error.Code).Should().Contain([
            "PasswordPolicy.Uppercase",
            "PasswordPolicy.Number",
            "PasswordPolicy.SpecialCharacter"]);
    }

    [Fact]
    public async Task Accepts_a_password_that_meets_the_effective_policy()
    {
        Mock<IAuthenticationSecurityPolicyProvider> provider = new();
        provider.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        SecurityPolicyPasswordValidator validator = new(provider.Object);

        IdentityResult result = await validator.ValidateAsync(null!, new User(), "Abcdef1!");

        result.Succeeded.Should().BeTrue();
    }
}
