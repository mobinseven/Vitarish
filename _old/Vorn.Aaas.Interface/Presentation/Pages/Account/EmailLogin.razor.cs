namespace Vorn.Aaas.Interface.Presentation.Pages.Account;

public partial class EmailLogin : IdentityBase
{

    protected string failureUrl => NavigationManager.GetUriWithQueryParameters("Account/Login/Email", new Dictionary<string, object?>
    {
        ["FailureMessage"] = Localizer["InvalidLoginAttempt"].Value,
        [nameof(ReturnUrl)] = ReturnUrl,
    });
    [SupplyParameterFromQuery]
    protected string? FailureMessage { get; set; }
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
