using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Accounts.Interface.Tests.TestInfrastructure;

internal sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(AnonymousState);
}
