namespace Vitarish.Accounts.Interface.Components.Account;

public partial class EmailChangeForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    private string? Email = null;
    private string? error = null;
    private async void ValidSendCodeSubmit(SendEmailCodeModel model)
    {
        if (User is null)
        {
            error = Localizer["UserNotFound"];
            NotifyStateChanged();
            return;
        }
        bool emailInUse = await UserService.CheckUserEmailInUseAsync(model.Email, User.Email);
        if (emailInUse)
        {
            error = Localizer["InvalidEmail"];
        }
        else
        {
            await UserService.SendEmailCode(User.Id, model.Email);
            Email = model.Email;
        }
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        if (hasRendered)
        {
            StateHasChanged();
        }
    }
}
