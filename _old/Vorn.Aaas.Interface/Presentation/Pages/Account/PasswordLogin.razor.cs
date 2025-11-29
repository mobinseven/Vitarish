namespace Vorn.Aaas.Interface.Presentation.Pages.Account;

public partial class PasswordLogin
{

    protected string failureUrl => NavigationManager.GetUriWithQueryParameters("Account/Login/Password", new Dictionary<string, object?>
    {
        [nameof(ReturnUrl)] = ReturnUrl,
    });

    private PasswordLoginModel Model { get; } = new();

    protected override async Task OnInitializedAsync()
    {
        if(HttpContext?.User.Identity?.IsAuthenticated ?? false)
        {
            RedirectManager.RedirectTo(ReturnUrl);
        }
        if(HttpMethods.IsGet(HttpContext?.Request.Method))
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }
}
