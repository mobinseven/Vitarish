namespace Vitarish.Accounts.Interface.Components.Account;

public partial class PhoneChangeForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    private System.Security.Claims.ClaimsPrincipal? ClaimsPrincipal = null;
    private User? User = null;
    private string? PhoneNumber = null;
    private string? error = null;
    private async Task ValidSendCodeSubmit(SendSmsCodeModel model)
    {
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
                IEnumerable<User> users = await UserService.GetUsersAsync();
                if (users.Any(u => u.Id != User.Id && u.PhoneNumber == model.PhoneNumber))
                {
                    error = Localizer["InvalidPhoneNumber"];
                }
                else
                {
                    await SmsAuthenticationService.SendCode(User, model.PhoneNumber);
                    error = null;
                    PhoneNumber = model.PhoneNumber;
                }
            }
        }
        else
        {
            throw new InvalidOperationException("Non-authenticated user accessing a authenticated user form");
        }
        await InvokeAsync(StateHasChanged);
    }

}
