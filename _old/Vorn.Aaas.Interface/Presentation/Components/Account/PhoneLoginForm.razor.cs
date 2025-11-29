using System.Security.Claims;

namespace Vorn.Aaas.Interface.Presentation.Components.Account;

public partial class PhoneLoginForm : CommonBase
{
    [Parameter] public string VerificationEndpoint { get; set; } = "/Account/Login/Phone/Submit";
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    User? User = null;
    string? PhoneNumber = null;

    async Task ValidSendCodeSubmit(SendSmsCodeModel model)
    {
        ClaimsPrincipal claimsPrincipal = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
        if(!claimsPrincipal.Identity?.IsAuthenticated ?? true)
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
