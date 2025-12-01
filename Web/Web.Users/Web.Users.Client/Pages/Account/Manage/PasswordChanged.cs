namespace Vitarish.Web.Users.Client.Pages.Account.Manage;

[Route("/Account/Manage/PasswordChanged")]
public class PasswordChanged : IdentityBase
{
    [SupplyParameterFromQuery] protected string? Id { get; set; }
    protected override async Task OnInitializedAsync()
    {
        if(!string.IsNullOrEmpty(Id))
        {
            User? user = await UserService.GetUserAsync(Id);
            if(user is null)
            {
                RedirectManager.RedirectTo(WebUtility.UrlDecode(FailureUrl));
                return;
            }
            await SignInManager.RefreshSignInAsync(user);
            RedirectManager.RedirectTo(WebUtility.UrlDecode(ReturnUrl));
        }
        else
        {
            RedirectManager.RedirectTo(WebUtility.UrlDecode(FailureUrl));
        }
    }
}
