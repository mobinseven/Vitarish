using Vitarish.Interfaces.Accounts.Components;

namespace Vitarish.Main.Pages.Identity;

public partial class EmailLogin : IdentityBase
{
    [SupplyParameterFromQuery]
    protected string? ReturnUrl { get; set; }
    
    [SupplyParameterFromQuery]
    protected string? FailureMessage { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        // Store ReturnUrl in cookie if provided via query parameter
        if (!string.IsNullOrWhiteSpace(ReturnUrl))
        {
            ReturnUrlManager.SetReturnUrl(ReturnUrl);
        }
        
        if (HttpContext?.User.Identity?.IsAuthenticated ?? false)
        {
            RedirectManager.RedirectTo(ReturnUrlManager.GetReturnUrl());
        }
        if (HttpMethods.IsGet(HttpContext?.Request.Method))
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

}
