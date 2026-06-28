namespace Vitarish.Main.Pages.Identity;

public partial class Logout : IdentityBase
{

    [SupplyParameterFromQuery]
    private string? logoutId { get; set; }
    public bool LoggedOut { get; set; }
    public string? PostLogoutRedirectUri { get; set; }
    public string? SignOutIframeUrl { get; set; }


    private sealed class InputModel
    {
        public string? returnUrl { get; set; }
    }
    private InputModel Input { get; set; } = new();
    protected override async Task OnInitializedAsync()
    {
        Duende.IdentityServer.Models.LogoutRequest? request = await InteractionService.GetLogoutContextAsync(logoutId);
        if (request?.ShowSignoutPrompt == false || (!HttpContext?.User?.Identity?.IsAuthenticated ?? false))
        {
            await LogoutUser();
        }
    }
    private async Task LogoutUser()
    {
        if (SignInManager.IsSignedIn(SignInManager.Context.User))
        {
            await SignInManager.SignOutAsync();
        }

        if (logoutId is not null)
        {
            Duende.IdentityServer.Models.LogoutRequest? request = await InteractionService.GetLogoutContextAsync(logoutId);
            if (request != null)
            {
                PostLogoutRedirectUri = request.PostLogoutRedirectUri;
                SignOutIframeUrl = request.SignOutIFrameUrl;
            }
        }
        LoggedOut = true;
    }

}
