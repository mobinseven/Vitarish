namespace Vorn.Aaas.Interface.Presentation.Components.Account;

public partial class EmailChangeForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    System.Security.Claims.ClaimsPrincipal? ClaimsPrincipal = null;
    User? User = null;
    string? Email = null;
    string? error = null;

    async void ValidSendCodeSubmit(SendEmailCodeModel model)
    {
        ClaimsPrincipal = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
        if(ClaimsPrincipal.Identity?.IsAuthenticated ?? false)
        {
            string? id = ClaimsPrincipal.FindFirstValue("sub");
            if(id is not null)
            {
                User = await UserService.GetUserAsync(id);
            }
            if(User is not null)
            {
                var users = await UserService.GetUsersAsync();
                if(users.Any(u => u.Id != User.Id && u.Email == model.Email))
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

    protected async Task ValidVerifyCodeSubmit(VerifyEmailCodeModel model)
    {
        ClaimsPrincipal = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
        if(ClaimsPrincipal!.Identity?.IsAuthenticated ?? false)
        {
            if(await EmailAuthenticationService.VerifyCode(User!, model.Code))
            {
                Dictionary<string, object?> queryParams = new()
                {
                    [nameof(User.Id)] = User!.Id,
                    [nameof(Email)] = CipherService.Encrypt(Email!),
                    [nameof(ReturnUrl)] = WebUtility.UrlEncode(ReturnUrl ?? ""),
                    [nameof(FailureUrl)] = WebUtility.UrlEncode(FailureUrl ?? ""),
                };
                RedirectManager.RedirectTo("/Account/Manage/EmailVerified", queryParams);
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
