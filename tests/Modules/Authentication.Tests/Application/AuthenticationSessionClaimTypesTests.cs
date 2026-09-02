using System.Security.Claims;
using FluentAssertions;
using Vitarish.Authentication.Application.Abstractions;

namespace Authentication.Tests.Application;

public sealed class AuthenticationSessionClaimTypesTests
{
    [Fact]
    public void Finds_name_identifier_when_sub_is_absent()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")]));

        AuthenticationSessionClaimTypes.FindSubject(principal).Should().Be("user-1");
    }

    [Fact]
    public void Prefers_canonical_sub_when_both_subject_claims_exist()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "identity-user"),
            new Claim(AuthenticationSessionClaimTypes.Subject, "canonical-user")
        ]));

        AuthenticationSessionClaimTypes.FindSubject(principal).Should().Be("canonical-user");
    }
}
