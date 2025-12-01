using System.Security.Claims;

namespace Vitarish.Web.Users.Client.Components.Account;

public partial class EmailLoginForm : CommonBase
{
    [Parameter] public string VerificationEndpoint { get; set; } = "/Account/Login/Email/Submit";
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    User? User = null;
    string? Email = null;

    async void ValidSendCodeSubmit(SendEmailCodeModel model)
    {
        ClaimsPrincipal claimsPrincipal = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
        if(!claimsPrincipal.Identity?.IsAuthenticated ?? true)
        {
            User = await EmailAuthenticationService.GetOrRegisterUser(model.Email);
            await EmailAuthenticationService.SendCode(User, model.Email);
            Email = model.Email;
        }
        else
        {
            throw new InvalidOperationException("Authenticated user accessing a non-authenticated user form");
        }
        StateHasChanged();
    }
}
