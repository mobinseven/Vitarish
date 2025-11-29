namespace Vorn.Aaas.Interface.Presentation.Components.Account;

public partial class StatusMessage : IdentityBase
{
    private string? messageFromCookie;

    private string? DisplayMessage => messageFromCookie;

    protected override void OnInitialized()
    {
        messageFromCookie = HttpContext?.Request.Cookies[IdentityRedirectManager.StatusCookieName];

        if(messageFromCookie is not null)
        {
            HttpContext?.Response.Cookies.Delete(IdentityRedirectManager.StatusCookieName);
        }
    }

}
