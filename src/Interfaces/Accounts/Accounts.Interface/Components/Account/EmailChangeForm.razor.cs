namespace Vitarish.Accounts.Interface.Components.Account;

public partial class EmailChangeForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    private string? Email = null;
    private string? error = null;
    private User? User = null;
    private bool hasRendered = false;

    protected override void OnAfterRender(bool firstRender)
    {
        hasRendered = true;
    }

    private async Task ValidSendCodeSubmit(SendEmailCodeModel model)
    {
        User = await UserService.GetUserAsync(model.User?.Id);
        if (User is null)
        {
            error = Localizer["UserNotFound"];
            NotifyStateChanged();
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
