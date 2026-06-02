using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Vitarish.Main.Mock.Authentication;

public sealed class PreviewAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal PreviewUser =
        PreviewAuthenticationDefaults.CreatePrincipal(authenticationType: "Preview");

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(PreviewUser));
}
