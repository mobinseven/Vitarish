using Vitarish.Interfaces.Accounts.Components.Base;

namespace Vitarish.Interfaces.Accounts.Components.Account;

public partial class StatusMessage : IdentityBase
{
    private string? DisplayMessage { get; set; }

    protected override Task OnInitializedAsync()
    {
        DisplayMessage = HttpContext?.Request.Cookies[IdentityRedirectManager.StatusCookieName];

        if (DisplayMessage is not null)
        {
            HttpContext?.Response.Cookies.Delete(IdentityRedirectManager.StatusCookieName);
        }
        return base.OnInitializedAsync();
    }

}
