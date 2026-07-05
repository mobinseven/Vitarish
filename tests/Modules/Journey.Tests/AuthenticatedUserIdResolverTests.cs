using System.Security.Claims;
using FluentAssertions;
using Vitarish.Modules.Base.Core.Identity;

namespace Journey.Tests;

public sealed class AuthenticatedUserIdResolverTests
{
    [Fact]
    public void Resolve_returns_subject_claim_for_bearer_principal()
    {
        var principal = CreatePrincipal(new Claim(AuthenticatedUserIdResolver.SubjectClaimType, "bearer-user"));

        AuthenticatedUserIdResolution result = AuthenticatedUserIdResolver.Resolve(principal);

        result.Should().BeOfType<AuthenticatedUserIdResolution.Success>()
            .Which.UserId.Value.Should().Be("bearer-user");
    }

    [Fact]
    public void Resolve_returns_name_identifier_claim_for_cookie_principal()
    {
        var principal = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "cookie-user"));

        AuthenticatedUserIdResolution result = AuthenticatedUserIdResolver.Resolve(principal);

        result.Should().BeOfType<AuthenticatedUserIdResolution.Success>()
            .Which.UserId.Value.Should().Be("cookie-user");
    }

    [Fact]
    public void Resolve_returns_explicit_failure_when_subject_id_is_missing()
    {
        var principal = CreatePrincipal(new Claim(ClaimTypes.Email, "user@example.com"));

        AuthenticatedUserIdResolution result = AuthenticatedUserIdResolver.Resolve(principal);

        result.Should().BeOfType<AuthenticatedUserIdResolution.MissingSubjectId>();
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Test"));
}
