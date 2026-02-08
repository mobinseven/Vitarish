namespace Vitarish.Interfaces.Accounts.Components;

public partial class StatusMessage : IdentityBase
{
    private string? DisplayMessage { get; set; }

    protected override Task OnInitializedAsync()
    {
        DisplayMessage = HttpContext?.Request.Cookies[IdentityCookieNames.StatusCookieName];

        if (DisplayMessage is not null)
        {
            HttpContext?.Response.Cookies.Delete(IdentityCookieNames.StatusCookieName);
        }
        return base.OnInitializedAsync();
    }

}
