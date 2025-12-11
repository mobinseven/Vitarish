namespace Vitarish.Interface.Presentation.Pages.Account.Manage;

[Route("/Account/Manage/PhoneVerified")]
public class PhoneVerified : IdentityBase
{
    [SupplyParameterFromQuery] protected string? PhoneNumber { get; set; }
    [SupplyParameterFromQuery] protected string? Id { get; set; }
    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(Id))
        {
            User? user = await UserService.GetUserAsync(Id);
            if (user is not null && PhoneNumber is not null)
            {
                PhoneNumber = CipherService.Decrypt(PhoneNumber);
                if (user.PhoneNumber != PhoneNumber)
                {
                    IdentityResult setPhoneResult = await UserService.SetUserPhoneNumberAsync(user.Id, PhoneNumber);
                    if (!setPhoneResult.Succeeded)
                    {
                        await ((IUserPhoneNumberStore<User>)UserStore).SetPhoneNumberConfirmedAsync(user, true, default);
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
