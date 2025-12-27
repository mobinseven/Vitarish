namespace Vitarish.Accounts.Interface.Components.Account;

public partial class PhoneLoginForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    private User? User = null;
    private string? PhoneNumber = null;

    private async Task ValidSendCodeSubmit(SendSmsCodeModel model)
    {
        ClaimsPrincipal claimsPrincipal = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
        if (!claimsPrincipal.Identity?.IsAuthenticated ?? true)
        {
            User = await SmsAuthenticationService.GetOrRegisterUser(model.PhoneNumber);
            await SmsAuthenticationService.SendCode(User, model.PhoneNumber);
            PhoneNumber = model.PhoneNumber;
        }
        else
        {
            throw new InvalidOperationException("Authenticated user accessing a non-authenticated user form");
        }

        StateHasChanged();
    }
}
