namespace Vitarish.Accounts.Interface.Components.Account;

public partial class EmailChangeForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    private System.Security.Claims.ClaimsPrincipal? ClaimsPrincipal = null;
    private User? User = null;
    private string? Email = null;
    private string? error = null;

    private async void ValidSendCodeSubmit(SendEmailCodeModel model)
    {
        error = null;
        ClaimsPrincipal = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
        if (ClaimsPrincipal.Identity?.IsAuthenticated ?? false)
        {
            string? id = ClaimsPrincipal.FindFirstValue("sub");
            if (id is not null)
            {
                User = await UserService.GetUserAsync(id);
            }
            if (User is not null)
            {
                bool emailInUse = await UserService.CheckUserEmailInUseAsync(model.Email, User.Id);
                if (emailInUse)
                {
                    error = Localizer["InvalidEmail"];
                }
                else
                {
                    await EmailAuthenticationService.SendCode(User, model.Email);
                    Email = model.Email;
                }
            }
        }
        else
        {
            throw new InvalidOperationException("Non-authenticated user accessing a authenticated user form");
        }
        StateHasChanged();
    }
}
