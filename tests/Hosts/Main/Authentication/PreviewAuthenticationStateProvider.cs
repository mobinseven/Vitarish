using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Vitarish.Accounts.Domain.Constants;

namespace Vitarish.Main.Mock.Authentication;

public sealed class PreviewAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal PreviewUser = new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Name, "preview-user"),
        new Claim("sub", "preview-user"),
        new Claim(AaasConstants.AccessClaim, "admin.local:::Admin")
    ],
    authenticationType: "Preview"));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(PreviewUser));
    }
}
