namespace Vitarish.Interface.Presentation.Components.Account;

public partial class PhoneChangeForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    private System.Security.Claims.ClaimsPrincipal? ClaimsPrincipal = null;
    private User? User = null;
    private string? PhoneNumber = null;
    private string? error = null;
    private async void ValidSendCodeSubmit(SendSmsCodeModel model)
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
                var users = await UserService.GetUsersAsync();
                if (users.Any(u => u.Id != User.Id && u.PhoneNumber == model.PhoneNumber))
                {
                    error = Localizer["InvalidPhoneNumber"];
                }
                else
                {
                    await SmsAuthenticationService.SendCode(User, model.PhoneNumber);
                    PhoneNumber = model.PhoneNumber;
                }
            }
        }
        else
        {
            throw new InvalidOperationException("Non-authenticated user accessing a authenticated user form");
        }
        StateHasChanged();
    }
    protected async Task ValidVerifyCodeSubmit(VerifySmsCodeModel model)
    {
        ClaimsPrincipal = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
        if (ClaimsPrincipal!.Identity?.IsAuthenticated ?? false)
        {
            if (await SmsAuthenticationService.VerifyCode(User!, model.Code))
            {
                Dictionary<string, object?> queryParams = new()
                {
                    [nameof(User.Id)] = User!.Id,
                    [nameof(PhoneNumber)] = CipherService.Encrypt(PhoneNumber!),
                    [nameof(ReturnUrl)] = WebUtility.UrlEncode(ReturnUrl ?? ""),
                    [nameof(FailureUrl)] = WebUtility.UrlEncode(FailureUrl ?? ""),
                };
                RedirectManager.RedirectTo("/Account/Manage/PhoneVerified", queryParams);
            }
            else
            {
                error = Localizer["WrongCode"];
            }
        }
        else
        {
            throw new InvalidOperationException("Non-authenticated user accessing a authenticated user form");
        }
        StateHasChanged();
    }

}
