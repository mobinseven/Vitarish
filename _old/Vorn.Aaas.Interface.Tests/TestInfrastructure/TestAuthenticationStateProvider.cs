using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Vorn.Aaas.Interface.Tests.TestInfrastructure;

internal sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(AnonymousState);
}
