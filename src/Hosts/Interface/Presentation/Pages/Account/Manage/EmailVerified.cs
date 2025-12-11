namespace Vitarish.Interface.Presentation.Pages.Account.Manage;

[Route("/Account/Manage/EmailVerified")]
public class EmailVerified : IdentityBase
{
    [SupplyParameterFromQuery] protected string? Email { get; set; }
    [SupplyParameterFromQuery] protected string? Id { get; set; }
    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(Id))
        {
            User? user = await UserService.GetUserAsync(Id);
            if (user is not null && Email is not null)
            {
                Email = CipherService.Decrypt(Email);
                if (user.Email != Email)
                {
                    IdentityResult setEmailResult = await UserService.SetUserEmailAsync(user.Id, Email);
                    if (!setEmailResult.Succeeded)
                    {
                        await ((IUserEmailStore<User>)UserStore).SetEmailConfirmedAsync(user, true, default);
                        RedirectManager.RedirectTo(WebUtility.UrlDecode(FailureUrl));
                    }
                    else
                    {
                        await SignInManager.RefreshSignInAsync(user);
                        RedirectManager.RedirectTo(WebUtility.UrlDecode(ReturnUrl));
                    }
                }
                else
                {
                    RedirectManager.RedirectTo(WebUtility.UrlDecode(ReturnUrl));
                }
            }
        }
        RedirectManager.RedirectTo(WebUtility.UrlDecode(FailureUrl));
    }
}
