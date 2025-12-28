namespace Vitarish.Accounts.Interface.Components.Account;

public partial class EmailChangeForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    private string? Email = null;
    private string? error = null;
    private User? User = null;
    private async void ValidSendCodeSubmit(SendEmailCodeModel model)
    {
        User = await UserService.GetUserAsync();
        if (User is null)
        {
            error = Localizer["UserNotFound"];
            StateHasChanged();
            return;
        }
        bool emailInUse = await UserService.CheckUserEmailInUseAsync(model.Email);
        if (emailInUse)
        {
            error = Localizer["InvalidEmail"];
        }
        else
        {
            await UserService.SendEmailCode(model.Email);
            Email = model.Email;
        }
        StateHasChanged();
    }
}
