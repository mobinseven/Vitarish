namespace Vitarish.Main.Pages.Identity;

public partial class PasswordLogin
{
    private PasswordLoginModel Model { get; } = new();

    protected override async Task OnInitializedAsync()
    {
        if (HttpContext?.User.Identity?.IsAuthenticated ?? false)
        {
            RedirectManager.RedirectTo(ReturnUrl);
        }
        if (HttpMethods.IsGet(HttpContext?.Request.Method))
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }
}
