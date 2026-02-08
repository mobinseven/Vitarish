using Microsoft.AspNetCore.Authentication;

namespace Vitarish.Interfaces.Accounts.Components;

public class LoginBase : IdentityBase
{
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
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
