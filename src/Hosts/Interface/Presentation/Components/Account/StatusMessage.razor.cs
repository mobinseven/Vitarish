namespace Vitarish.Interface.Presentation.Components.Account;

public partial class StatusMessage : IdentityBase
{
    private string? DisplayMessage { get; set; }

    protected override void OnInitialized()
    {
        DisplayMessage = HttpContext?.Request.Cookies[IdentityRedirectManager.StatusCookieName];

        if (DisplayMessage is not null)
        {
            HttpContext?.Response.Cookies.Delete(IdentityRedirectManager.StatusCookieName);
        }
    }

}
