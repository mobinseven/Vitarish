namespace Vitarish.Main.Pages.Account;

public partial class PhoneLogin
{

    protected string failureUrl => NavigationManager.GetUriWithQueryParameters("Account/Login/Phone", new Dictionary<string, object?>
    {
        ["FailureMessage"] = Localizer["InvalidLoginAttempt"].Value,
        [nameof(ReturnUrl)] = ReturnUrl,
    });
    [SupplyParameterFromQuery]
    protected string? FailureMessage { get; set; }
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
